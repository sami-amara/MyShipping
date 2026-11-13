using DataAccessLayer.UserModels;
using Microsoft.AspNetCore.Identity;
using UI.Constants;

namespace UI.Services
{
    public static class IdentitySeeder
    {
        private static readonly string[] Roles =
            { "User", RoleNames.Admin, RoleNames.Reviewer, RoleNames.Operation, RoleNames.OperationManager };

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Optional first admin, only when configured (User Secrets / environment variables).
            var email = configuration["SeedAdmin:Email"];
            var password = configuration["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
            if (await userManager.FindByEmailAsync(email) is not null) return;

            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User"
            };

            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                logger.LogError("Admin seeding failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Code)));
                return;
            }
            await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        }
    }
}