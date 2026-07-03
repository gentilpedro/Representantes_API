namespace Josapar.Api.Infrastructure.Persistence.Entities;

public class Representative
{
    public Guid Id { get; set; }
    public required string MatriculaCode { get; set; }
    public string? Email { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
    public required string Region { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActivated { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastSyncAtUtc { get; set; }
}
