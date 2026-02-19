namespace AmlOps.Backend.Application.Contracts;

public sealed record LoginRequest(string Username, string Password, string? TenantId, string? Role);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid TenantId,
    Guid UserId,
    string Role,
    string Username
);
