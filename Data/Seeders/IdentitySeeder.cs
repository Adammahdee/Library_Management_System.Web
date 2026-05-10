using Library_Management_System.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Library_Management_System.Web.Data.Seeders
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAndAdminAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService
                <RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService
                <UserManager<ApplicationUser>>();




            string[] roles =
            {
                "Admin",
                "Librarian",
                "Student",
                "Lecturer"
            };



            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }




            var adminEmail =
                "admin@library.com";

            var adminUser =
                await userManager.FindByEmailAsync(adminEmail);




            if (adminUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };



                var result =
                    await userManager.CreateAsync(
                        user,
                        "Admin@123"
                    );



                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(
                        user,
                        "Admin"
                    );
                }
            }
        }
    }
}