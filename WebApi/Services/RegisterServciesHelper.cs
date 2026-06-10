using Business.Contracts;
using Business.Contracts.Shipment;
using Business.Mapping;
using Business.Services;
using Business.Services.Shipment;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Contracts;
using DataAccessLayer.DbContext;
using DataAccessLayer.Repositories;
using DataAccessLayer.UserModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Text;
using System.Threading.RateLimiting;
using System.Globalization;

namespace WebApi.Services
{
    public static class RegisterServicesHelper
    {
        public static void RegisterServices(WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            var env = builder.Environment;

            // ═══════════════════════════════════════════════════════════════
            // CORS - MUST BE FIRST
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("https://localhost:7065") // ✅ Exact origin
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // ✅ Critical for cookies!
                });
            });

            // ═══════════════════════════════════════════════════════════════
            // Database Context
            // ═══════════════════════════════════════════════════════════════
            var conn = builder.Configuration.GetConnectionString("ShippingConnection");
            if (string.IsNullOrWhiteSpace(conn))
                throw new Exception("Connection string 'ShippingConnection' is missing");

            Console.WriteLine($"ConnectionString: {conn}");

            builder.Services.AddDbContext<ShippingContext>(options =>
                options.UseSqlServer(conn)
                       .EnableSensitiveDataLogging(env.IsDevelopment())
                       .EnableDetailedErrors()
                       .LogTo(Console.WriteLine,
                              new[] { Microsoft.EntityFrameworkCore.DbLoggerCategory.Database.Command.Name },
                              Microsoft.Extensions.Logging.LogLevel.Information));

            // ═══════════════════════════════════════════════════════════════
            // ✅ IDENTITY CORE (UserManager only, NO authentication schemes)
            // ═══════════════════════════════════════════════════════════════
            // ❌ REMOVED: builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            // ✅ REPLACED WITH:
            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddSignInManager()  // ✅ For UserService dependency
            .AddEntityFrameworkStores<ShippingContext>()
            .AddDefaultTokenProviders();

            // ❌ REMOVED: builder.Services.Configure<IdentityOptions>() 
            // (Options are now in AddIdentityCore above)

            // ═══════════════════════════════════════════════════════════════
            // Data Protection
            // ═══════════════════════════════════════════════════════════════
            var dpPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "MyShipping",
                "DataProtection-Keys");
            Directory.CreateDirectory(dpPath);

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dpPath))
                .SetApplicationName("MyShipping");

            // ═══════════════════════════════════════════════════════════════
            // ✅ JWT BEARER AUTHENTICATION ONLY (NO COOKIES!)
            // ═══════════════════════════════════════════════════════════════
            var jwtSecretKey = configuration.GetValue<string>("JwtSettings:SecretKey") ?? string.Empty;
            if (string.IsNullOrEmpty(jwtSecretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is missing from appsettings.json");
            }

            var key = Encoding.UTF8.GetBytes(jwtSecretKey);  // ✅ Use UTF8, not ASCII

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),

                        // ✅ Match your TokenService settings:
                        ValidateIssuer = true,
                        ValidIssuer = configuration["JwtSettings:Issuer"],

                        ValidateAudience = true,
                        ValidAudience = configuration["JwtSettings:Audience"],

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = ctx =>
                        {
                           
                            Console.WriteLine($"❌ JWT Auth Failed: {ctx.Exception?.Message}");
                            Console.WriteLine($"   Token: {ctx.Request.Headers["Authorization"]}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = ctx =>
                        {
                            Console.WriteLine($"✅ JWT validated for: {ctx.Principal?.Identity?.Name}");
                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            // ✅ Get token from query string for SignalR
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                              (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/yourHub")))
                            //path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        },
                        OnChallenge = ctx =>
                        {
                            // ✅ NEW: Log authentication challenges
                            Console.WriteLine($"⚠️ JWT Challenge: {ctx.Error} - {ctx.ErrorDescription}");
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // ═══════════════════════════════════════════════════════════════
            // Localization
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddLocalization();
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("ar")
                };

                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.ApplyCurrentCultureToResponseHeaders = true;

                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new QueryStringRequestCultureProvider(),
                    new CookieRequestCultureProvider(),
                    new AcceptLanguageHeaderRequestCultureProvider()
                };
            });

            // ═══════════════════════════════════════════════════════════════
            // HttpContext Accessor
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddHttpContextAccessor();

            // ═══════════════════════════════════════════════════════════════
            // HttpClient Factory
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddHttpClient("ApiClient", (sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var baseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:7228/";
                client.BaseAddress = new Uri(baseUrl);
            });

            // ═══════════════════════════════════════════════════════════════
            // Payment Gateway HttpClient Configuration
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddHttpClient("PayPal")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = 
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });

            builder.Services.AddHttpClient("Stripe")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = 
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });

            // ═══════════════════════════════════════════════════════════════
            // Rate Limiting
            // ═══════════════════════════════════════════════════════════════
          
            builder.Services.AddRateLimiter(options =>
            {
                // ✅ Global limiter for all API endpoints
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext =>
                    {
                        if (httpContext.Request.Path.StartsWithSegments("/health") ||
                            httpContext.Request.Path.StartsWithSegments("/swagger"))
                        {
                            return RateLimitPartition.GetNoLimiter<string>("unlimited");
                        }

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 100,  // General API: 100 req/min
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0
                            });
                    });

                // ✅ Strict policy for login/register
                options.AddPolicy("auth", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,  // Login/Register: 10 attempts/15 min
                            Window = TimeSpan.FromMinutes(15),
                            QueueLimit = 0
                        }));

                // ✅ NEW: Moderate policy for token refresh
                options.AddPolicy("token-refresh", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,  // Token refresh: 20 attempts/15 min (more lenient)
                            Window = TimeSpan.FromMinutes(15),
                            QueueLimit = 0
                        }));
                // ════════════════════════════════════════════════════════════
                // ✅ NEW: FORM SUBMISSION POLICY (Create, Edit, Delete)
                // ════════════════════════════════════════════════════════════
                options.AddPolicy("form-submit", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,                           // ✅ 20 operations
                            Window = TimeSpan.FromMinutes(1),           // ✅ Per minute
                            QueueLimit = 0
                        }));
                // OnRejected handler
                //options.OnRejected = async (context, cancellationToken) => { ... };
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = "Your Account has been locked, Too many requests. Please try again later."
                    }, cancellationToken);
                };
            });
            // ═══════════════════════════════════════════════════════════════
            // Serilog
            // ═══════════════════════════════════════════════════════════════
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs/security-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();

            builder.Host.UseSerilog();

            // ═══════════════════════════════════════════════════════════════
            // AutoMapper
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

            // ═══════════════════════════════════════════════════════════════
            // Repository & Services
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped(typeof(IViewRepository<>), typeof(ViewRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IShippingTypes, ShippingTypeService>();
            builder.Services.AddScoped<ICountry, CountryService>();
            builder.Services.AddScoped<ICity, CityService>();
            builder.Services.AddScoped<IUserSender, UserSenderService>();
            builder.Services.AddScoped<IUserReceiver, UserRceiverService>();

            builder.Services.AddScoped<IShipmentCommand, ShipmentCommandService>();
            builder.Services.AddScoped<IShipmentQuery, ShipmentQueryServic>();


           
            builder.Services.AddScoped<IShipmentsStatus, ShipmentsStatusService>();
            builder.Services.AddScoped<ISubscriptionPackage, SbuscriptionPackageService>();
            builder.Services.AddScoped<IRefreshToken, RefreshTokenService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRateCalculator, RateCalculatorService>();
            builder.Services.AddScoped<ITrackingNumberCreator, TrackingNumberCreatorService>();
            builder.Services.AddScoped<IPaymentMethods, PaymentMethodService>();
            builder.Services.AddScoped<IPaymentTransactionService, PaymentTransactionService>();
            builder.Services.AddScoped<IShippingPackage, ShipingPackgingService>();
            builder.Services.AddScoped<IRefreshTokenRetriver, RefreshTokenRetriverService>();
            builder.Services.AddScoped<ICarrier, CarrierService>();
            builder.Services.AddScoped<SecurityEventLogger>();
            builder.Services.AddScoped<IEmailSender, EmailSenderService>();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddScoped<GenericApiClient>();

            builder.Services.AddScoped<IShipmentStateHandlerFactory, ShipmentStateHandlerFactory>();
            builder.Services.AddScoped<CreatedShipment>();
            builder.Services.AddScoped<ApproveShipment>();
            builder.Services.AddScoped<UpdateShipment>();
            builder.Services.AddScoped<ReadyForShippingShipment>();
            builder.Services.AddScoped<ShippedShipment>();
            builder.Services.AddScoped<DelivredShipment>();
            builder.Services.AddScoped<CancelledShipment>();
            builder.Services.AddScoped<ReturnedShipment>();
            builder.Services.AddScoped<DeletedShipment>();

            //// ═══════════════════════════════════════════════════════════════
            //// Payment Gateway Configuration & Services
            //// ═══════════════════════════════════════════════════════════════
            builder.Services.Configure<Business.Configuration.PaymentGatewayOptions>(
                configuration.GetSection("PaymentGateways"));

            builder.Services.AddScoped<Business.Services.PaymentGateways.StripePaymentGateway>();
            builder.Services.AddScoped<Business.Services.PaymentGateways.PayPalPaymentGateway>();
            builder.Services.AddScoped<Business.Contracts.IPaymentGatewayFactory,
                Business.Services.PaymentGateways.PaymentGatewayFactory>();


            // ═══════════════════════════════════════════════════════════════
            // SignalR
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = env.IsDevelopment();
                options.MaximumReceiveMessageSize = 128 * 1024;
            });

            // ═══════════════════════════════════════════════════════════════
            // Swagger
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApi", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your JWT token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }
    }
}

