using LogiEdge.Data;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Identity
{
    /// <summary>
    /// Seeds the Admin role and local test users so login and role-based authorization can be
    /// exercised in Development:
    /// - admin@localhost / admin  (Admin role)
    /// - user@localhost / 123456  (no roles)
    /// - system                   (locked out, no password — attribution target for background actions, D4)
    /// Idempotent: does nothing when the role and users already exist; assigns the Admin role to
    /// an existing 'admin@localhost' on first run after this change.
    /// </summary>
    public static class TestUserSeeder
    {
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            // The identity context is not migrated anywhere else at startup; ensure the schema exists.
            await using AppIdentityDbContext dbContext =
                scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
            await dbContext.Database.MigrateAsync();

            RoleManager<IdentityRole> roleManager =
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<IdentityUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            if (await roleManager.FindByNameAsync(AppRoles.Admin) is null)
            {
                IdentityResult roleResult = await roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));
                if (!roleResult.Succeeded)
                {
                    string errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    app.Logger.LogWarning("Seeding role '{Role}' failed: {Errors}", AppRoles.Admin, errors);
                    return;
                }
            }

            IdentityUser? adminUser = await userManager.FindByNameAsync("admin@localhost")
                ?? await CreateTestUserAsync(app, userManager, "admin@localhost", "admin");
            if (adminUser is null)
            {
                return;
            }

            // A user can hold multiple roles (AspNetUserRoles is many-to-many); this only
            // guarantees the Admin role is present, without touching any others.
            if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
            {
                IdentityResult assignment = await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                if (!assignment.Succeeded)
                {
                    string errors = string.Join("; ", assignment.Errors.Select(e => e.Description));
                    app.Logger.LogWarning("Assigning role '{Role}' to 'admin@localhost' failed: {Errors}", AppRoles.Admin, errors);
                }
            }

            // Second test user without any role: verifies that non-admins can sign in and use the
            // app, but are blocked from admin-only surfaces.
            if (await userManager.FindByNameAsync("user@localhost") is null)
            {
                await CreateTestUserAsync(app, userManager, "user@localhost", "123456");
            }

            // Per D4 (.notes/auth-impl.md): locked-out "system" user that background actions outside of a UI
            // circuit (e.g. the Excel import) are attributed to, so CreatedByUserId/AuthorId are always real ids.
            if (await userManager.FindByNameAsync(AppUsers.System) is null)
            {
                IdentityUser systemUser = new()
                {
                    UserName = AppUsers.System,
                    LockoutEnabled = true,
                    LockoutEnd = DateTimeOffset.MaxValue, // locked out permanently; no password set → cannot sign in
                };

                IdentityResult result = await userManager.CreateAsync(systemUser);
                if (!result.Succeeded)
                {
                    string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    app.Logger.LogWarning("Seeding system user '{UserName}' failed: {Errors}", AppUsers.System, errors);
                }
            }
        }

        private static async Task<IdentityUser?> CreateTestUserAsync(
            WebApplication app, UserManager<IdentityUser> userManager, string userName, string password)
        {
            IdentityUser user = new()
            {
                UserName = userName,
                Email = userName,
            };

            IdentityResult result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                app.Logger.LogWarning("Seeding test user '{UserName}' failed: {Errors}", userName, errors);
                return null;
            }

            return user;
        }
    }
}
