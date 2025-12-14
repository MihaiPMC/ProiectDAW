// Data/SeedData.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using ProiectDAW.Models;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string adminRole = "Administrator";
        string editorRole = "Editor";
        string userRole = "User";

        // Creează rolurile dacă nu există
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        if (!await roleManager.RoleExistsAsync(editorRole))
        {
            await roleManager.CreateAsync(new IdentityRole(editorRole));
        }

        if (!await roleManager.RoleExistsAsync(userRole))
        {
            await roleManager.CreateAsync(new IdentityRole(userRole));
        }

        // Creează un utilizator admin inițial (schimbă email/parola din appsettings sau aici)
        var adminEmail = "robertocristianbaciu@gmail.com";
        var adminPwd = "Admin123!"; // schimbă în ceva sigur sau ia din config

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true // dacă RequireConfirmedAccount este true, confirmăm manual în seed
            };
            var result = await userManager.CreateAsync(adminUser, adminPwd);
            if (!result.Succeeded)
            {
                throw new Exception("Eroare la crearea admin user: " + string.Join(", ", result.Errors));
            }
        }

        // Adaugă utilizatorul în rolul Administrator
        if (!await userManager.IsInRoleAsync(adminUser, adminRole))
        {
            await userManager.AddToRoleAsync(adminUser, adminRole);
        }
    }
}