using DataAccessLayer.DbContext;
using DataAccessLayer.UserModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text;
using UI.Services;
using Microsoft.AspNetCore.Localization;

namespace UI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            RegisterServicesHelper.RegisterServices(builder);

            builder.Services.AddControllersWithViews();
            //builder.Services.AddRazorPages();

            var app = builder.Build();

            var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
            var sendGridApiKey = app.Configuration["SendGrid:ApiKey"];
            var sendGridFromEmail = app.Configuration["SendGrid:FromEmail"];
            var sendGridFromName = app.Configuration["SendGrid:FromName"];

            if (string.IsNullOrWhiteSpace(sendGridApiKey) || sendGridApiKey.Contains("YOUR-API-KEY", StringComparison.OrdinalIgnoreCase))
            {
                startupLogger.LogWarning("SendGrid:ApiKey is missing or placeholder. Email sending is not configured.");
            }

            if (string.IsNullOrWhiteSpace(sendGridFromEmail))
            {
                startupLogger.LogWarning("SendGrid:FromEmail is missing. Email sending may fail.");
            }

            if (string.IsNullOrWhiteSpace(sendGridFromName))
            {
                startupLogger.LogWarning("SendGrid:FromName is missing. Default sender name will be used.");
            }

            // ═══════════════════════════════════════════════════════════════
            // ✅ MIDDLEWARE ORDER - CRITICAL!
            // ═══════════════════════════════════════════════════════════════

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseStatusCodePagesWithReExecute("/Error/{0}");
                app.UseHsts();
            }

            app.Use(async (context, next) =>
            {
                  // ✅ ADD THIS: Strict security headers
                  context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                  context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
                  context.Response.Headers.Append("X-XSS-Protection", "0");
                  context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                  context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

                  var csp = "default-src 'self'; " +
                            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://www.paypal.com https://www.sandbox.paypal.com https://js.stripe.com; " +
                            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
                            "img-src 'self' data: https:; " +
                            "font-src 'self' data: https://fonts.gstatic.com; " +
                            "connect-src 'self' https://localhost:7228 https://www.paypal.com https://www.sandbox.paypal.com https://api.stripe.com; " +
                            "frame-src 'self' https://www.paypal.com https://www.sandbox.paypal.com https://js.stripe.com; " +
                            "child-src 'self' https://www.paypal.com https://www.sandbox.paypal.com; " +
                            "report-uri /api/security/csp-violation;";

                  context.Response.Headers.Append("Content-Security-Policy", csp);

                  await next();
            });


            app.UseHttpsRedirection();
            app.UseStaticFiles();

            var uiLocalizationOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(uiLocalizationOptions);

            app.UseRouting();

            // ✅ CORS before authentication
            app.UseCors("AllowWebApi");

            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "admin",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            //app.MapRazorPages();

            // Database migration
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShippingContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                await dbContext.Database.MigrateAsync();
              // await ContextConfigruration.SeedDataAsync(dbContext, userManager, roleManager);
            }

            await app.RunAsync();
        }
    }
}









