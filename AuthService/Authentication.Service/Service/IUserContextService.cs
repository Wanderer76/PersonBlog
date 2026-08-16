using Shared.Models;

namespace Authentication.Service.Service;

/// <summary>
/// Stores a context already authorized by a trusted caller and selects it for the user session.
/// Resource-specific access checks must be completed before calling this service.
/// </summary>
public interface IUserContextService
{
    Task<Result<UserModel>> GrantAndActivateAsync(
        Guid userId,
        string contextType,
        Guid contextId,
        Guid? roleId = null,
        CancellationToken cancellationToken = default);
}
