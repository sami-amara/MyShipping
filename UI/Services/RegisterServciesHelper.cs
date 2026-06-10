using AutoMapper;
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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;

namespace UI.Services
{
    public static class RegisterServicesHelper
    {
        public static void RegisterServices(WebApplicationBuilder builder)
        {
            var config = builder.Configuration;
            var env = builder.Environment;

            // ═══════════════════════════════════════════════════════════════
            // CORS Configuration
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWebApi", policy =>
                {
                    policy.WithOrigins("https://localhost:7228")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // ═══════════════════════════════════════════════════════════════
            // HttpClient Configuration with Cookie Support
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                var apiBase = config["ApiSettings:BaseUrl"] ?? "https://localhost:7228/";
                client.BaseAddress = new Uri(apiBase);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer(),
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
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

            builder.Services.AddHttpContextAccessor();

            // ═══════════════════════════════════════════════════════════════
            // Data Protection (Shared Keys with WebApi)
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
            // Database Context
            // ═══════════════════════════════════════════════════════════════
            var conn = config.GetConnectionString("ShippingConnection");
            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException(
                    "Connection string 'ShippingConnection' is missing. " +
                    "Check appsettings.json / secrets / environment variables.");

            Console.WriteLine($"ConnectionString: {conn}");

            builder.Services.AddDbContext<ShippingContext>(options =>
                options.UseSqlServer(conn)
                       .EnableSensitiveDataLogging(env.IsDevelopment())
                       .EnableDetailedErrors()
                       .LogTo(Console.WriteLine,
                              new[] { Microsoft.EntityFrameworkCore.DbLoggerCategory.Database.Command.Name },
                              Microsoft.Extensions.Logging.LogLevel.Information));

            // ═══════════════════════════════════════════════════════════════
            // ✅ IDENTITY (includes SignInManager automatically)
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ShippingContext>()
            .AddDefaultTokenProviders();

            // ✅ ADD THIS BLOCK - Force Identity to include roles in claims
            builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
                UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>>();

            // Antiforgery Cookies
            // ═══════════════════════════════════════════════════════════════
            // Antiforgery Cookies
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".AspNetCore.Antiforgery";
            });


            // ═══════════════════════════════════════════════════════════════
            // ✅ CONFIGURE Identity's Cookie (NOT a new authentication scheme)
            // ═══════════════════════════════════════════════════════════════
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "MyShipping.AuthCookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SessionStore = null;

                options.Events = new CookieAuthenticationEvents
                {
                    OnSigningIn = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILoggerFactory>()
                            ?.CreateLogger("CookieAuth");

                        var claims = context.Principal?.Claims
                            .Select(c => $"{c.Type}={c.Value.Substring(0, Math.Min(15, c.Value.Length))}")
                            .ToList();

                        logger?.LogInformation("🔐 OnSigningIn: Claims={Claims}",
                            string.Join(", ", claims ?? new List<string>()));

                        return Task.CompletedTask;
                    },
                    OnValidatePrincipal = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILoggerFactory>()
                            ?.CreateLogger("CookieAuth");

                        var hasToken = context.Principal?.FindFirst("AccessToken") != null;
                        logger?.LogInformation(
                            "✅ ValidatePrincipal: User={Name}, HasToken={HasToken}",
                            context.Principal?.Identity?.Name,
                            hasToken);

                        return Task.CompletedTask;
                    },
                    OnRedirectToLogin = ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") ||
                            ctx.Request.Path.StartsWithSegments("/hubs"))
                        {
                            ctx.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        }
                        ctx.Response.Redirect(ctx.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            // ═══════════════════════════════════════════════════════════════
            // Localization
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddLocalization(options =>
                options.ResourcesPath = "Resources");

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
            // Rate Limiting
            // ═══════════════════════════════════════════════════════════════
           

            ////// UI\Program.cs (or UI\Services\RegisterServicesHelper.cs)

            builder.Services.AddRateLimiter(options =>
            {
                // Define "login" policy
                options.AddPolicy("login", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10, // ← 10 attempts allowed
                            Window = TimeSpan.FromMinutes(5),
                            QueueLimit = 0
                        }));
                // Define what happens when limit exceeded
                options.AddPolicy("form-submit", httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 20,                          // ✅ 20 form submissions
                                Window = TimeSpan.FromMinutes(1),          // ✅ Per minute
                                QueueLimit = 0
                            }));
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var retryAfterSeconds = 300; // Default 1 minute

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        retryAfterSeconds = (int)retryAfter.TotalSeconds;
                    }

                    context.HttpContext.Response.StatusCode = 429;

                    // ✅ IMPROVED: Better detection of AJAX/API calls
                    var request = context.HttpContext.Request;
                    var isApiCall =
                        request.Path.StartsWithSegments("/api") ||
                        request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        request.ContentType?.Contains("application/json") == true;

                    if (isApiCall)
                    {
                        // ✅ Return JSON for API/AJAX calls
                        context.HttpContext.Response.ContentType = "application/json";
                        await context.HttpContext.Response.WriteAsJsonAsync(new
                        {
                            success = false,
                            message = $"Too many requests. Please try again in {Math.Ceiling(retryAfterSeconds / 60.0)} minutes.",
                            retryAfterSeconds = retryAfterSeconds,
                            retryAfterMinutes = (int)Math.Ceiling(retryAfterSeconds / 60.0)
                        }, cancellationToken);
                    }
                    else
                    {
                        // ✅ Redirect for traditional form posts
                        var tempDataProvider = context.HttpContext.RequestServices
                            .GetRequiredService<ITempDataDictionaryFactory>();
                        var tempData = tempDataProvider.GetTempData(context.HttpContext);

                        tempData["RateLimitExceeded"] = true;
                        tempData["RetryAfterSeconds"] = retryAfterSeconds;
                        tempData["RetryAfterMinutes"] = (int)Math.Ceiling(retryAfterSeconds / 60.0);
                        tempData.Save();

                        var referer = context.HttpContext.Request.Headers["Referer"].ToString();
                        var redirectUrl = !string.IsNullOrEmpty(referer) &&
                                          referer.Contains(context.HttpContext.Request.Host.Value)
                            ? referer
                            : "/Account/Login";

                        context.HttpContext.Response.Redirect(redirectUrl);
                    }
                };
                
            });

            // ═══════════════════════════════════════════════════════════════
            // AutoMapper
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddAutoMapper(cfg =>
                cfg.AddProfile<MappingProfile>());

            // ═══════════════════════════════════════════════════════════════
            // Repository & Unit of Work
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped(typeof(IViewRepository<>), typeof(ViewRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ═══════════════════════════════════════════════════════════════
            // API Client
            // ═══════════════════════════════════════════════════════════════
            builder.Services.AddScoped<GenericApiClient>();

            // ═══════════════════════════════════════════════════════════════
            // Business Services
            // ═══════════════════════════════════════════════════════════════
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
            //builder.Services.AddScoped<IUserService, UserService>();
            // ✅ CHANGED: Register as IUIUserService (UI-specific)
            builder.Services.AddScoped<IUIUserService, UserService>();

            // ✅ ALSO register as IUserService for backward compatibility if needed
            builder.Services.AddScoped<IUserService>(sp => sp.GetRequiredService<IUIUserService>());


            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IRateCalculator, RateCalculatorService>();
            builder.Services.AddScoped<ITrackingNumberCreator, TrackingNumberCreatorService>();
            builder.Services.AddScoped<IPaymentMethods, PaymentMethodService>();
            builder.Services.AddScoped<IPaymentTransactionService, PaymentTransactionService>();
            builder.Services.AddScoped<IShippingPackage, ShipingPackgingService>();
            builder.Services.AddScoped<IRefreshTokenRetriver, RefreshTokenRetriverService>();
            builder.Services.AddScoped<ICarrier, CarrierService>();
            builder.Services.AddScoped<SecurityEventLogger>();
            builder.Services.AddScoped<IEmailSender, EmailSenderService>();
            builder.Services.AddScoped<AccountAuditService>();

            builder.Services.AddScoped<AccountHelper>();  // ✅ NEW: Add AccountHelper
            builder.Services.AddScoped<IShipmentStateHandlerFactory, ShipmentStateHandlerFactory>();
            builder.Services.AddScoped<ApproveShipment>();
            builder.Services.AddScoped<UpdateShipment>();
            builder.Services.AddScoped<ReadyForShippingShipment>();
            builder.Services.AddScoped<ShippedShipment>();
            builder.Services.AddScoped<DelivredShipment>();
            builder.Services.AddScoped<ReturnedShipment>();

            // ═══════════════════════════════════════════════════════════════
            // Payment Gateway Configuration & Services
            // ═══════════════════════════════════════════════════════════════
            builder.Services.Configure<Business.Configuration.PaymentGatewayOptions>(
                config.GetSection("PaymentGateways"));

            builder.Services.AddScoped<Business.Services.PaymentGateways.StripePaymentGateway>();
            builder.Services.AddScoped<Business.Services.PaymentGateways.PayPalPaymentGateway>();
            builder.Services.AddScoped<Business.Contracts.IPaymentGatewayFactory, 
                Business.Services.PaymentGateways.PaymentGatewayFactory>();
        }
    }
}

















