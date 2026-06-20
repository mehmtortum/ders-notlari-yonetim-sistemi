using DersNotlari.Api.Models;
using DersNotlari.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DersNotlari.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();

        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var (hash, salt) = passwordService.HashPassword("Demo123!");
        var user = new AppUser
        {
            FullName = "Demo Kullanici",
            Email = "demo@tetacode.com",
            PasswordHash = hash,
            PasswordSalt = salt,
            Notes =
            [
                new Note
                {
                    CourseName = "ASP.NET Core",
                    Description = "Web API, JWT kimlik dogrulama ve Entity Framework Core notlari."
                },
                new Note
                {
                    CourseName = "React.js",
                    Description = "Bilesenler, state yonetimi ve API entegrasyonu calisma notlari."
                },
                new Note
                {
                    CourseName = "SQL Server",
                    Description = "Migration, seeder ve soft delete sorgulari icin ornek not."
                }
            ]
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
