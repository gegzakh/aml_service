using System.Security.Claims;

namespace AmlOps.Backend.Infrastructure;

public sealed class CurrentRequestContext(IHttpContextAccessor accessor)
{
    public Guid TenantId => ParseClaimOrHeader("tenant_id", "x-tenant-id", "00000000-0000-0000-0000-000000000001");
    public Guid UserId => ParseClaimOrHeader(ClaimTypes.NameIdentifier, "x-user-id", "00000000-0000-0000-0000-000000000111");
    public string[] Roles
    {
        get
        {
            var claimRoles = accessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray();
            if (claimRoles is { Length: > 0 })
            {
                return claimRoles;
            }

            var headers = accessor.HttpContext?.Request.Headers["x-roles"].ToString();
            return string.IsNullOrWhiteSpace(headers)
                ? ["ComplianceAdmin"]
                : headers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    private Guid ParseClaimOrHeader(string claimName, string header, string fallback)
    {
        var claimValue = accessor.HttpContext?.User.FindFirstValue(claimName);
        if (Guid.TryParse(claimValue, out var claimParsed))
        {
            return claimParsed;
        }

        var headerValue = accessor.HttpContext?.Request.Headers[header].ToString();
        if (Guid.TryParse(headerValue, out var headerParsed))
        {
            return headerParsed;
        }

        return Guid.Parse(fallback);
    }
}
