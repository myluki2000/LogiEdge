using LogiEdge.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Identity
{
    /// <summary>
    /// Seeds a local test user (admin@localhost / admin) so login can be exercised in Development.
    /// Idempotent: does nothing when the user already exists.
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

            UserManager<IdentityUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            if (await userManager.FindByNameAsync("admin@localhost") is not null)
            {
                return;
            }

            IdentityUser user = new()
            {
                UserName = "admin@localhost",
                Email = "admin@localhost",
                // RequireConfirmedAccount is enabled and no email infrastructure exists,
                // so confirm the account directly to make it sign-in capable.
                EmailConfirmed = true,
            };

            IdentityResult result = await userManager.CreateAsync(user, "admin");
            if (!result.Succeeded)
            {
                string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                app.Logger.LogWarning("Seeding test user 'admin@localhost' failed: {Errors}", errors);
            }
        }
    }
}
