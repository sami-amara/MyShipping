using DataAccessLayer.DbContext;
using DataAccessLayer.UserModels;
using Microsoft.AspNetCore.Identity;

namespace UI.Services
{
    public class ContextConfigruration
    {
        private static readonly string seedAdminEmail = "Wasan@gmail.com";
        private static readonly string seedReviewerEmail = "reviewer@gmail.com";
        private static readonly string seedOperationEmail = "operation@gmail.com";
        private static readonly string seedOperationManagerEmail = "opmanager@gmail.com";

        public static async Task SeedDataAsync(ShippingContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            await SeedRolesAsync(roleManager);
            await SeedUsersAsync(userManager);
            await PaymentMethodSeeder.SeedAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Ensure roles exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // New roles
            if (!await roleManager.RoleExistsAsync("Reviewer"))
            {
                                              
            await roleManager.CreateAsync(new IdentityRole("Reviewer"));
            }

            if (!await roleManager.RoleExistsAsync("Operation"))
            {
                await roleManager.CreateAsync(new IdentityRole("Operation"));
            }

            if (!await roleManager.RoleExistsAsync("OperationManager"))
            {
                await roleManager.CreateAsync(new IdentityRole("OperationManager"));
            }
        }

        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
        {
            // Seed Admin User
            await SeedSingleUserAsync(userManager, seedAdminEmail, "Admin", "Wasan", "Assad", "Wasan/123456", "079793155");

            // Seed Reviewer User
            await SeedSingleUserAsync(userManager, seedReviewerEmail, "Reviewer", "Reviewer", "User", "Reviewer/123456", "0791111111");

            // Seed Operation User
            await SeedSingleUserAsync(userManager, seedOperationEmail, "Operation", "Operation", "User", "Operation/123456", "0792222222");

            // Seed OperationManager User
            await SeedSingleUserAsync(userManager, seedOperationManagerEmail, "OperationManager", "OperationManager", "User", "OpManager/123456", "0793333333");
        }

        private static async Task SeedSingleUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string role,
            string firstName,
            string lastName,
            string password,
            string phoneNumber)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = phoneNumber // Added Phone property here
                };
                var result = await userManager.CreateAsync(newUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, role);
                }
            }
        }
    }
}
