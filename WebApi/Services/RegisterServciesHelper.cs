using Business.Configuration;
using Business.Contracts;
using Business.Contracts.Logging;
using Business.Contracts.Payments;
using Business.Contracts.Shipment;
using Business.Mapping;
using Business.Services;
using Business.Services.Logging;
using Business.Services.Logging.Filters;
using Business.Services.Logging.Middleware;
using Business.Services.Logging.SecurityLogger;
using Business.Services.Shipment;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Contracts;
using DataAccessLayer.DbContext;
using DataAccessLayer.Repositories;
using DataAccessLayer.UserModels;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using WebApi.Logging.SecurityLogger;


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

            //Console.WriteLine($"ConnectionString: {conn}");

            builder.Services.AddDbContext<ShippingContext>(options =>
                options.UseSqlServer(conn)
                       .EnableSensitiveDataLogging(false)  // Disabled: too verbose in logs, security risk
                       .EnableDetailedErrors());
                       // .LogTo(Console.WriteLine,  // COMMENTED: Removed EF query logging to reduce noise
                       //        new[] { Microsoft.EntityFrameworkCore.DbLoggerCategory.Database.Command.Name },
                       //        Microsoft.Extensions.Logging.LogLevel.Warning);

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

            //// ✅ OpenTelemetry for distributed tracing and observability
            //builder.Services.AddOpenTelemetry(configuration, "MyShipping.WebApi");

            // 
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

         
            builder.Services.Configure<PaymentGatewayOptions>(
                configuration.GetSection("PaymentGateways"));

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
                // OnRejected handler - calls RateLimitBreachLogger
                options.OnRejected = async (context, cancellationToken) =>
                {
                    try
                    {
                        // Get the rate limit event logger from the service provider
                        var rateLimitLogger = context.HttpContext.RequestServices.GetRequiredService<IRateLimitEventLogger>();
                        var httpContext = context.HttpContext;

                        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        var correlationId = httpContext.Items.TryGetValue("CorrelationId", out var id) ? id?.ToString() ?? "unknown" : "unknown";

                        // Determine which policy was breached based on the route
                        var policyName = DeterminePolicyFromRoute(httpContext.Request.Path);

                        // Get cached email from HttpContextEnricher middleware
                        var email = HttpContextEnricher.GetEmail(httpContext) ?? "unknown";

                        await rateLimitLogger.LogRateLimitBreachAsync(
                            clientIdentifier: ipAddress,
                            endpoint: httpContext.Request.Path,
                            requestsCount: 1,
                            limitWindow: "1 minute");
                    }
                    catch { /* Swallow logging errors to avoid breaking the rejection response */ }

                    context.HttpContext.Response.StatusCode = 429;
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = "Your Account has been locked, Too many requests. Please try again later."
                    }, cancellationToken);
                };
            });


            builder.Services.AddMvc(options => 
            {
                // ✅ Exception filter now from Business layer via ConfigureBusinessLogging()
                options.Filters.AddService(typeof(ExceptionLoggingFilter));
            });



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
            builder.Services.AddScoped<IWebApiUserService, UserService>();  // ✅ MORE SPECIFIC
            builder.Services.AddScoped<IRateCalculator, RateCalculatorService>();
            builder.Services.AddScoped<ITrackingNumberCreator, TrackingNumberCreatorService>();
            builder.Services.AddScoped<IPaymentMethods, PaymentMethodService>();
            builder.Services.AddScoped<IPaymentTransactionService, PaymentTransactionService>();
            builder.Services.AddScoped<IShippingPackage, ShipingPackgingService>();
            builder.Services.AddScoped<IRefreshTokenRetriver, RefreshTokenRetriverService>();
            builder.Services.AddScoped<ICarrier, CarrierService>();
            // ✅ Register SecurityEventLogger via shared interface




            //builder.Services.AddScoped<ISecurityEventLogger, SecurityEventLogger>();

            builder.Services.AddScoped<ISecurityEventLogger>(sp =>
              new SecurityEventLogger(
                  sp.GetRequiredService<ILogger<SecurityEventLogger>>(),
                  sp.GetRequiredService<IHttpContextAccessor>(),
                  sp));  // 🔴 Pass IServiceProvider

            builder.Services.AddScoped<IShipmentAuditService, ShipmentAuditService>();
            builder.Services.AddScoped<ISecurityAuditLogRepository, SecurityAuditLogRepository>();


            builder.Services.AddScoped<Business.Contracts.IEmailSender, EmailSenderService>();
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
            builder.Services.AddScoped<RefundedShipment>();
            builder.Services.AddScoped<DeletedShipment>();



            builder.Services.AddScoped<SecurityAuditService>();
            builder.Services.AddSingleton<SecurityAuditTaskQueue>();
            builder.Services.AddHostedService<SecurityAuditBackgroundService>();

       


            builder.Services.AddScoped<Business.Services.PaymentGateways.StripePaymentGateway>();
            builder.Services.AddScoped<Business.Services.PaymentGateways.PayPalPaymentGateway>();
            builder.Services.AddScoped<IPaymentGatewayFactory,
            Business.Services.PaymentGateways.PaymentGatewayFactory>();


            // ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            // Serilog Configuration
            // ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            ConfigureSerilog(builder);


            // ✅ Register all Business layer logging and security services
            builder.Services.ConfigureBusinessLogging();
        }

        /// <summary>
        /// Determines the rate limit policy name based on the request path.
        /// </summary>
        private static string DeterminePolicyFromRoute(PathString path)
        {
            var pathValue = path.Value?.ToLower() ?? string.Empty;

            if (pathValue.Contains("/api/auth/login") || pathValue.Contains("/api/auth/register"))
                return "auth";

            if (pathValue.Contains("/api/auth/refresh"))
                return "token-refresh";

            if (pathValue.Contains("/api/shipments/create") || pathValue.Contains("/api/shipments/edit") || 
                pathValue.Contains("/api/shipments/delete"))
                return "form-submit";

            return "global";
        }


        /// <summary>
        /// Configures Serilog with environment-specific sinks and filtering.
        /// Mirrors the UI project configuration to ensure consistent audit trails across projects.
        /// </summary>
        public static void ConfigureSerilog(WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            var env = builder.Environment;

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "MyShipping.WebApi")
                .Enrich.WithProperty("Environment", env.EnvironmentName);

            // ✅ Apply per-namespace overrides
            var perNamespaceLevels = configuration.GetSection("Logging:PerNamespaceLogLevels").GetChildren();
            foreach (var levelConfig in perNamespaceLevels)
            {
                if (Enum.TryParse<LogEventLevel>(levelConfig.Value, out var level))
                {
                    loggerConfig = loggerConfig.MinimumLevel.Override(levelConfig.Key, level);
                }
            }

            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            // SINK 1: Security Log File (security-.txt)
            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            var securityLogPath = configuration.GetValue<string>("Logging:SecurityLogFilePath") ?? "Logs/security-.txt";
            loggerConfig = loggerConfig.WriteTo.Logger(lc => lc
                .Filter.With(new Business.Services.Logging.SecurityEventFilter())   // Only security events
                .Filter.With(new Business.Services.Logging.EmailMaskingFilter())    // Mask sensitive data
                .WriteTo.File(
                    securityLogPath,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 100_000_000,
                    rollOnFileSizeLimit: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
                )
            );

            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            // SINK 2: Application Log File (app-.txt)
            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            loggerConfig = loggerConfig.WriteTo.Logger(lc => lc
                .Filter.With(new Business.Services.Logging.NonSecurityEventFilter())  // Block security events
                .WriteTo.File(
                    "Logs/app-.txt",
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 10,
                    fileSizeLimitBytes: 50_000_000,
                    rollOnFileSizeLimit: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
            );

            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            // SINK 3: SQL Server Database (ONLY security events with masking)
            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            var sqlConnectionString = configuration.GetConnectionString("ShippingConnection");
            if (!string.IsNullOrEmpty(sqlConnectionString))
            {
                try
                {
                    var sinkOptions = new MSSqlServerSinkOptions
                    {
                        TableName = "Log",
                        //TableName = "SecurityAuditLogs",
                        SchemaName = "dbo",
                        AutoCreateSqlTable = false,
                        BatchPostingLimit = 100,
                        BatchPeriod = TimeSpan.FromSeconds(10),
                        EagerlyEmitFirstEvent = true
                    };

                    // ✅ SINGLE database sink with proper filtering
                    // Only writes security events (via SecurityEventFilter)
                    // All emails are masked (via EmailMaskingFilter)
                    loggerConfig = loggerConfig.WriteTo.Logger(lc => lc
                        .Filter.With(new Business.Services.Logging.SecurityEventFilter())   // ✅ Only security events
                        .Filter.With(new Business.Services.Logging.EmailMaskingFilter())    // ✅ Mask all emails
                        .WriteTo.MSSqlServer(
                            connectionString: sqlConnectionString,
                            sinkOptions: sinkOptions
                        )
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not configure SQL Server logging: {ex.Message}");
                }
            }

            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            // SINK 4: Application Insights (Production only)
            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            var appInsightsKey = configuration.GetValue<string>("ApplicationInsights:InstrumentationKey");
            if (!string.IsNullOrEmpty(appInsightsKey) && !env.IsDevelopment())
            {
                loggerConfig = loggerConfig.WriteTo.ApplicationInsights(
                    telemetryConverter: TelemetryConverter.Events,
                    restrictedToMinimumLevel: LogEventLevel.Warning);
            }

            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            // SINK 5: Console Output (Development only)
            // ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
            if (env.IsDevelopment())
            {
                loggerConfig = loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            Log.Logger = loggerConfig.CreateLogger();
            builder.Host.UseSerilog();
        }


    }
}

