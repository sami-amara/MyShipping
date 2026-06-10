using DataAccessLayer.DbContext;
using DataAccessLayer.UserModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Localization;
using WebApi.Hubs;
using WebApi.Services;

namespace WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
           

            var builder = WebApplication.CreateBuilder(args);

            // Register application services (DbContext, Identity, Auth, CORS, SignalR, AutoMapper, etc.)
            RegisterServicesHelper.RegisterServices(builder);

            // Add controllers and API explorer (Swagger already configured in RegisterServciesHelper)
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            //builder.Services.AddHttpClient();

            var app = builder.Build();

            var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
            var sendGridApiKey = app.Configuration["SendGrid:ApiKey"];
            var sendGridFromEmail = app.Configuration["SendGrid:FromEmail"];
            var sendGridFromName = app.Configuration["SendGrid:FromName"];

            if (string.IsNullOrWhiteSpace(sendGridApiKey) || sendGridApiKey.Contains("YOUR-API-KEY", StringComparison.OrdinalIgnoreCase))
            {
                startupLogger.LogWarning("SendGrid:ApiKey is missing or placeholder. Forgot-password emails will not be sent.");
            }

            if (string.IsNullOrWhiteSpace(sendGridFromEmail))
            {
                startupLogger.LogWarning("SendGrid:FromEmail is missing. Forgot-password emails may fail to send.");
            }

            if (string.IsNullOrWhiteSpace(sendGridFromName))
            {
                startupLogger.LogWarning("SendGrid:FromName is missing. Default sender name will be used.");
            }

           

            app.Use(async (context, next) =>
           {
                // ✅ ADD THIS: API-focused security headers
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "0");

                if (app.Environment.IsProduction())
                {
                    context.Response.Headers.Append("Strict-Transport-Security",
                        "max-age=31536000; includeSubDomains");
                }

                // Prevent caching of API responses
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.Headers.Append("Cache-Control", "no-store");
                }

                await next();
            });

            // 1. Exception handling
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }


            // 5. ✅ CORS (MUST BE AFTER UseRouting, BEFORE UseAuthentication)
            //app.UseCors("AllowFrontend");
            // 2. HTTPS redirection
            app.UseHttpsRedirection();

            // 3. Static files
            app.UseStaticFiles();

            var apiLocalizationOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(apiLocalizationOptions);

            // 4. Routing (BEFORE CORS)
            app.UseRouting();

            //// 5. ✅ CORS (MUST BE AFTER UseRouting, BEFORE UseAuthentication)
            app.UseCors("AllowFrontend");

            // 6. Rate limiting (BEFORE Authentication)
            app.UseRateLimiter();

            // 7. Authentication
            app.UseAuthentication();

            // 8. Authorization
            app.UseAuthorization();

            // 9. ✅ Map endpoints (SignalR hub MUST have CORS applied)
            app.MapHub<YourHub>("/hubs/yourHub")
               .RequireCors("AllowFrontend");  // ✅ CRITICAL

            app.MapControllers();

         

             app.Run();


        }
    }
}







