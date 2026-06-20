namespace DersNotlari.Api.Models;

public class Note
{
    public int Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public bool DeletedAtMarked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
}
