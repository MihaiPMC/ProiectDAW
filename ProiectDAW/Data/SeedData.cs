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

        string[] roleNames = { "Administrator", "Editor", "User" };
        IdentityResult roleResult;

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Admin User
        var adminEmail = "mihaipatru05@gmail.com";
        var adminPwd = "Admin123!"; 

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "MihaiPatru",
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Mihai",
                LastName = "Patru"
            };
            var createPowerUser = await userManager.CreateAsync(adminUser, adminPwd);
            if (createPowerUser.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
        }
        else
        {
            // Ensure admin has the role and correct password if user already exists
            if (!await userManager.IsInRoleAsync(adminUser, "Administrator"))
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
            
            // Optional: Reset password to ensure it works
            var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            await userManager.ResetPasswordAsync(adminUser, token, adminPwd);
        }

        // Editor User
        var editorEmail = "editor@test.com";
        var editorPwd = "Editor123!";

        var editorUser = await userManager.FindByEmailAsync(editorEmail);
        if (editorUser == null)
        {
            editorUser = new ApplicationUser
            {
                UserName = "TestEditor",
                Email = editorEmail,
                EmailConfirmed = true,
                FirstName = "Editor",
                LastName = "User",
                IsProfilePrivate = false // Explicitly public for testing feed
            };
            var createEditor = await userManager.CreateAsync(editorUser, editorPwd);
            if (createEditor.Succeeded)
            {
                await userManager.AddToRoleAsync(editorUser, "Editor");
            }
        }

        // Regular User
        var userEmail = "user@test.com";
        var userPwd = "User123!";

        var normalUser = await userManager.FindByEmailAsync(userEmail);
        if (normalUser == null)
        {
            normalUser = new ApplicationUser
            {
                UserName = "TestUser",
                Email = userEmail,
                EmailConfirmed = true,
                FirstName = "Regular",
                LastName = "User"
            };
            var createUser = await userManager.CreateAsync(normalUser, userPwd);
            if (createUser.Succeeded)
            {
                await userManager.AddToRoleAsync(normalUser, "User");
            }
        }
    }
}