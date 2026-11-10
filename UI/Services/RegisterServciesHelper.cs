using AutoMapper;
using Business.Contracts;
using Business.Contracts.Dashboard;
using Business.Contracts.Logging;
using Business.Contracts.Payments;
using Business.Contracts.Shipment;
using Business.Services.Logging;
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
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
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

                        //logger?.LogInformation("🔐 OnSigningIn: Claims={Claims}",
                        //    string.Join(", ", claims ?? new List<string>()));

                        logger?.LogDebug("🔐 OnSigningIn: Claims={Claims}",
                              string.Join(", ", claims ?? new List<string>()));
                        return Task.CompletedTask;
                    },
                    OnValidatePrincipal = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILoggerFactory>()
                            ?.CreateLogger("CookieAuth");

                        var hasToken = context.Principal?.FindFirst("AccessToken") != null;
                        if (!hasToken)
                        {
                            logger?.LogDebug(
                                "⚠️ ValidatePrincipal: User={Name} missing AccessToken",
                                context.Principal?.Identity?.Name);

                       
                        }
                      

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
                    var retryAfterSeconds = 300; // Default 5 minutes

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        retryAfterSeconds = (int)retryAfter.TotalSeconds;
                    }

                    context.HttpContext.Response.StatusCode = 429;

                    // 🆕 Track rate limit breach count per IP in HttpContext
                    var clientIdentifier = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                    // Initialize or increment the counter for this request
                    var counterKey = $"RateLimitCount_{clientIdentifier}";
                    int requestsCount = 1;

                    if (context.HttpContext.Items.ContainsKey(counterKey))
                    {
                        requestsCount = (int)context.HttpContext.Items[counterKey] + 1;
                    }
                    context.HttpContext.Items[counterKey] = requestsCount;

                    // Extract Email and CorrelationId for audit logging
                    string? email = null;
                    string? correlationId = null;

                    // Get CorrelationId from HttpContext.Items
                    if (context.HttpContext.Items.TryGetValue("CorrelationId", out var corrId))
                    {
                        correlationId = corrId?.ToString();
                    }

                    // Try to extract email from login form (POST to /Account/Login)
                    if (context.HttpContext.Request.Path == "/Account/Login" &&
                        context.HttpContext.Request.Method == "POST" &&
                        context.HttpContext.Request.ContentType?.Contains("application/x-www-form-urlencoded") == true)
                    {
                        try
                        {
                            context.HttpContext.Request.EnableBuffering();
                            var form = await context.HttpContext.Request.ReadFormAsync(cancellationToken);
                            email = form["Email"].FirstOrDefault() ?? form["email"].FirstOrDefault();
                            context.HttpContext.Request.Body.Position = 0;
                        }
                        catch
                        {
                            // Silently fail
                        }
                    }

                    // 🔒 Get rate limit logger and log the breach WITH dynamic requestsCount
                    var rateLimitLogger = context.HttpContext.RequestServices
                        .GetRequiredService<IRateLimitEventLogger>();

                    var endpoint = context.HttpContext.Request.Path.ToString();

                    // 🆕 PASS DYNAMIC requestsCount (increments per request)
                    await rateLimitLogger.LogRateLimitBreachAsync(
                        clientIdentifier,
                        endpoint,
                        requestsCount,  // 🆕 DYNAMIC - increments with each breach
                        "5 minutes",
                        email,
                        correlationId
                    );

                    // 🔒 Handle response (API vs Form)
                    var request = context.HttpContext.Request;
                    var isApiCall =
                        request.Path.StartsWithSegments("/api") ||
                        request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        request.ContentType?.Contains("application/json") == true;

                    if (isApiCall)
                    {
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
                        // Redirect for traditional form posts
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
            //builder.Services.AddScoped<ISecurityEventLogger, SecurityEventLogger>();
            // Line 458-459: Update these registrations:
            builder.Services.AddScoped<ISecurityEventLogger>(sp =>
                new SecurityEventLogger(
                    sp.GetRequiredService<ILogger<SecurityEventLogger>>(),
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    sp));  // 🔴 Pass IServiceProvider
            builder.Services.AddScoped<IShipmentAuditService, ShipmentAuditService>();
            builder.Services.AddScoped<ISecurityAuditLogRepository, SecurityAuditLogRepository>();
            
            //builder.Services.AddScoped<IRateLimitEventLogger, RateLimitEventLogger>();
            builder.Services.AddScoped<IRateLimitEventLogger>(sp =>
            new RateLimitEventLogger(
            sp.GetRequiredService<ILogger<RateLimitEventLogger>>(),
            sp));  // 🔴 Pass IServiceProvider

            // Add the background task queue
            builder.Services.AddSingleton<SecurityAuditTaskQueue>();

            // Add the background service
            builder.Services.AddHostedService<SecurityAuditBackgroundService>();

            builder.Services.AddScoped<IEmailSender, EmailSenderService>();
            builder.Services.AddScoped<AccountAuditService>();
            builder.Services.AddScoped<SecurityAuditService>();

            builder.Services.AddScoped<AccountHelper>();  // ✅ NEW: Add AccountHelper
            builder.Services.AddScoped<IShipmentStateHandlerFactory, ShipmentStateHandlerFactory>();
            builder.Services.AddScoped<ApproveShipment>();
            builder.Services.AddScoped<UpdateShipment>();
            builder.Services.AddScoped<ReadyForShippingShipment>();
            builder.Services.AddScoped<ShippedShipment>();
            builder.Services.AddScoped<DelivredShipment>();
            builder.Services.AddScoped<RefundedShipment>();
            builder.Services.AddScoped<ReturnedShipment>(); 


    


            // ═══════════════════════════════════════════════════════════════
            // Payment Gateway Configuration & Services
            // ═══════════════════════════════════════════════════════════════
            builder.Services.Configure<Business.Configuration.PaymentGatewayOptions>(
                config.GetSection("PaymentGateways"));

            builder.Services.AddScoped<Business.Services.PaymentGateways.StripePaymentGateway>();
            builder.Services.AddScoped<Business.Services.PaymentGateways.PayPalPaymentGateway>();
            builder.Services.AddScoped<IPaymentGatewayFactory, 
                Business.Services.PaymentGateways.PaymentGatewayFactory>();

            // ✅ Register all Business layer logging and security services
            builder.Services.ConfigureBusinessLogging();
        }

        /// <summary>
        /// Configure Serilog for UI application with centralized security logging.
        /// Mirrors WebApi configuration to ensure consistent audit trails across projects.
        /// </summary>
        /// 


        public static void ConfigureSerilog(WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            var env = builder.Environment;

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "MyShipping.UI")
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

            // ════════════════════════════════════════════════════════════════════════════════════════
            // SINK 1: Security Log File (security-.txt)
            // ════════════════════════════════════════════════════════════════════════════════════════
            var securityLogPath = configuration.GetValue<string>("Logging:SecurityLogFilePath") ?? "Logs/security-.txt";
            loggerConfig = loggerConfig.WriteTo.Logger(lc => lc
                .Filter.With(new Business.Services.Logging.SecurityEventFilter())   // Only security events
                .Filter.With(new EmailMaskingFilter())                     // Mask sensitive data
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

            // ════════════════════════════════════════════════════════════════════════════════════════
            // SINK 2: Application Log File (app-.txt)
            // ════════════════════════════════════════════════════════════════════════════════════════
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

            // ════════════════════════════════════════════════════════════════════════════════════════
            // SINK 3: SQL Server Database (ONLY security events with masking)
            // ════════════════════════════════════════════════════════════════════════════════════════
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
                        .Filter.With(new EmailMaskingFilter())                     // ✅ Mask all emails
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

            // ════════════════════════════════════════════════════════════════════════════════════════
            // SINK 4: Console Output (Development only)
            // ════════════════════════════════════════════════════════════════════════════════════════
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

















