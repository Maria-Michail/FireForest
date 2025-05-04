using ForestFireWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ForestFireWebApp.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<UserPreference> UserPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserPreference>()
                .HasIndex(p => new { p.UserId, p.State })
                .IsUnique();

            builder.Entity<UserPreference>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);
        }

        public static async Task SeedTestUserAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var testUserName = "testuser";
            var testPassword = "Test@123";

            var existingUser = await userManager.FindByNameAsync(testUserName);
            if (existingUser == null)
            {
                var user = new ApplicationUser { UserName = testUserName };
                var result = await userManager.CreateAsync(user, testPassword);

                if (result.Succeeded)
                {
                    Console.WriteLine("Test user created successfully!");
                }
                else
                {
                    Console.WriteLine("Failed to create test user:");
                    foreach (var error in result.Errors)
                        Console.WriteLine($"- {error.Description}");
                }
            }
        }

    }
}
