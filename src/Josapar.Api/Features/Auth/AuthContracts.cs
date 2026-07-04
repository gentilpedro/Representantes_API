namespace Josapar.Api.Features.Auth;

public record LoginRequest(string Identifier, string Password);

public record FirstAccessCheckRequest(string Identifier);

public record ActivateAccountRequest(string Identifier, string Password);

public record RepresentativeResponse(
    string Id,
    string Name,
    string Role,
    string Region,
    string? AvatarUrl,
    DateTime? LastSyncAtUtc);

public record AuthResponse(string Token, RepresentativeResponse Representative);

public record FirstAccessCheckResponse(string Name);

public record PasswordPolicyErrorResponse(IReadOnlyList<string> UnmetRequirements);
