using AmlOps.Backend.Application.Contracts;

namespace AmlOps.Backend.Application;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
