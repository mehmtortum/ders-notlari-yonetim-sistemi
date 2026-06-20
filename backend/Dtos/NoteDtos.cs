namespace DersNotlari.Api.Dtos;

public record NoteResponse(
    int Id,
    string CourseName,
    string Description,
    string? FileName,
    string? FileUrl,
    string? ContentType,
    long? FileSize,
    bool DeletedAtMarked,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt);
