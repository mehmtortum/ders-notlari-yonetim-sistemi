using System.Security.Claims;
using System.Text;
using DersNotlari.Api.Data;
using DersNotlari.Api.Dtos;
using DersNotlari.Api.Models;
using DersNotlari.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 25 * 1024 * 1024;
});

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("Frontend");
app.UseStaticFiles();
var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "Uploads");
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/Uploads"
});
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    AppDbContext db,
    PasswordService passwordService,
    TokenService tokenService) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Ad, e-posta ve sifre zorunludur." });
    }

    var email = request.Email.Trim().ToLowerInvariant();
    if (await db.Users.AnyAsync(user => user.Email == email))
    {
        return Results.Conflict(new { message = "Bu e-posta adresi zaten kayitli." });
    }

    var (hash, salt) = passwordService.HashPassword(request.Password);
    var user = new AppUser
    {
        FullName = request.FullName.Trim(),
        Email = email,
        PasswordHash = hash,
        PasswordSalt = salt
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created("/api/auth/register", new AuthResponse(tokenService.CreateToken(user), user.FullName, user.Email));
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    AppDbContext db,
    PasswordService passwordService,
    TokenService tokenService) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(item => item.Email == email);

    if (user is null || !passwordService.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new AuthResponse(tokenService.CreateToken(user), user.FullName, user.Email));
});

app.MapGet("/api/notes", async (ClaimsPrincipal principal, AppDbContext db, HttpContext http) =>
{
    var userId = GetUserId(principal);
    var notes = await db.Notes
        .Where(note => note.AppUserId == userId && !note.DeletedAtMarked)
        .OrderByDescending(note => note.UpdatedAt)
        .ToListAsync();

    return Results.Ok(notes.Select(note => ToResponse(note, http)));
}).RequireAuthorization();

app.MapGet("/api/notes/archive", async (ClaimsPrincipal principal, AppDbContext db, HttpContext http) =>
{
    var userId = GetUserId(principal);
    var notes = await db.Notes
        .Where(note => note.AppUserId == userId && note.DeletedAtMarked)
        .OrderByDescending(note => note.DeletedAt)
        .ToListAsync();

    return Results.Ok(notes.Select(note => ToResponse(note, http)));
}).RequireAuthorization();

app.MapPost("/api/notes", async (
    ClaimsPrincipal principal,
    HttpRequest request,
    AppDbContext db) =>
{
    var form = await request.ReadFormAsync();
    var note = new Note
    {
        AppUserId = GetUserId(principal),
        CourseName = Required(form["courseName"], "Ders adi"),
        Description = Required(form["description"], "Aciklama")
    };

    await AttachFileAsync(note, form.Files.GetFile("file"), uploadsRoot);
    db.Notes.Add(note);
    await db.SaveChangesAsync();

    return Results.Created($"/api/notes/{note.Id}", ToResponse(note, request.HttpContext));
}).RequireAuthorization();

app.MapPut("/api/notes/{id:int}", async (
    int id,
    ClaimsPrincipal principal,
    HttpRequest request,
    AppDbContext db) =>
{
    var userId = GetUserId(principal);
    var note = await db.Notes.SingleOrDefaultAsync(item => item.Id == id && item.AppUserId == userId);

    if (note is null)
    {
        return Results.NotFound();
    }

    var form = await request.ReadFormAsync();
    note.CourseName = Required(form["courseName"], "Ders adi");
    note.Description = Required(form["description"], "Aciklama");
    note.UpdatedAt = DateTime.UtcNow;

    await AttachFileAsync(note, form.Files.GetFile("file"), uploadsRoot);
    await db.SaveChangesAsync();

    return Results.Ok(ToResponse(note, request.HttpContext));
}).RequireAuthorization();

app.MapDelete("/api/notes/{id:int}", async (int id, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = GetUserId(principal);
    var note = await db.Notes.SingleOrDefaultAsync(item => item.Id == id && item.AppUserId == userId && !item.DeletedAtMarked);

    if (note is null)
    {
        return Results.NotFound();
    }

    note.DeletedAtMarked = true;
    note.DeletedAt = DateTime.UtcNow;
    note.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/api/notes/{id:int}/restore", async (int id, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = GetUserId(principal);
    var note = await db.Notes.SingleOrDefaultAsync(item => item.Id == id && item.AppUserId == userId && item.DeletedAtMarked);

    if (note is null)
    {
        return Results.NotFound();
    }

    note.DeletedAtMarked = false;
    note.DeletedAt = null;
    note.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/api/notes/{id:int}/hard", async (int id, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = GetUserId(principal);
    var note = await db.Notes.SingleOrDefaultAsync(item => item.Id == id && item.AppUserId == userId && item.DeletedAtMarked);

    if (note is null)
    {
        return Results.NotFound();
    }

    if (!string.IsNullOrWhiteSpace(note.FilePath))
    {
        var path = Path.Combine(app.Environment.ContentRootPath, note.FilePath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    db.Notes.Remove(note);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

await SeedData.InitializeAsync(app.Services);
app.Run();

static int GetUserId(ClaimsPrincipal principal)
{
    var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
    return int.TryParse(subject, out var userId) ? userId : throw new UnauthorizedAccessException();
}

static string Required(string? value, string fieldName)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new BadHttpRequestException($"{fieldName} zorunludur.");
    }

    return value.Trim();
}

static async Task AttachFileAsync(Note note, IFormFile? file, string uploadsRoot)
{
    if (file is null || file.Length == 0)
    {
        return;
    }

    var allowed = new[] { ".pdf", ".doc", ".docx", ".txt", ".png", ".jpg", ".jpeg" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowed.Contains(extension))
    {
        throw new BadHttpRequestException("Sadece PDF, Word, metin ve gorsel dosyalari yuklenebilir.");
    }

    var storedName = $"{Guid.NewGuid():N}{extension}";
    var absolutePath = Path.Combine(uploadsRoot, storedName);

    await using var stream = File.Create(absolutePath);
    await file.CopyToAsync(stream);

    note.FileName = Path.GetFileName(file.FileName);
    note.FilePath = Path.Combine("Uploads", storedName);
    note.ContentType = file.ContentType;
    note.FileSize = file.Length;
}

static NoteResponse ToResponse(Note note, HttpContext http)
{
    var fileUrl = string.IsNullOrWhiteSpace(note.FilePath)
        ? null
        : $"{http.Request.Scheme}://{http.Request.Host}/{note.FilePath.Replace("\\", "/")}";

    return new NoteResponse(
        note.Id,
        note.CourseName,
        note.Description,
        note.FileName,
        fileUrl,
        note.ContentType,
        note.FileSize,
        note.DeletedAtMarked,
        note.CreatedAt,
        note.UpdatedAt,
        note.DeletedAt);
}
