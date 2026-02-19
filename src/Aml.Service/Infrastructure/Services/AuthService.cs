using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AmlOps.Backend.Application;
using AmlOps.Backend.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AmlOps.Backend.Infrastructure.Services;

public sealed class AuthService(IConfiguration configuration) : IAuthService
{
    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Username and password are required.");
        }

        // MVP placeholder validation. Replace with OIDC/IdP integration in production.
        if (!string.Equals(request.Password, "Pass@123", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var tenantId = Guid.TryParse(request.TenantId, out var parsedTenant)
            ? parsedTenant
            : Guid.Parse("00000000-0000-0000-0000-000000000001");

        var userId = BuildDeterministicUserId(request.Username);
        var role = string.IsNullOrWhiteSpace(request.Role) ? "ComplianceAdmin" : request.Role;

        var issuer = configuration["Jwt:Issuer"] ?? "amlops-local";
        var audience = configuration["Jwt:Audience"] ?? "amlops-frontend";
        var secret = configuration["Jwt:SecretKey"] ?? "CHANGE_ME_TO_A_LONG_RANDOM_DEV_KEY_123456789";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, request.Username),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, request.Username),
            new(ClaimTypes.Role, role),
            new("tenant_id", tenantId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(new LoginResponse(jwt, expiresAt, tenantId, userId, role, request.Username));
    }

    private static Guid BuildDeterministicUserId(string username)
    {
        var bytes = Encoding.UTF8.GetBytes(username.ToLowerInvariant());
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }
}
