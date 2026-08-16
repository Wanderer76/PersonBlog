using Authentication.Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Utils;

namespace Authentication.Service.Service.Implementation;

internal sealed class DefaultUserContextService : IUserContextService
{
    private readonly IReadWriteRepository<IAuthEntity> _repository;
    private readonly ICacheService _cacheService;

    public DefaultUserContextService(
        IReadWriteRepository<IAuthEntity> repository,
        ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<Result<UserModel>> GrantAndActivateAsync(
        Guid userId,
        string contextType,
        Guid contextId,
        Guid? roleId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = UserContext.NormalizeContextType(contextType);
        var user = await _repository.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .Include(x => x.UserContexts)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
            return Result<UserModel>.Failure(new Error("NotFound", "User was not found."));

        if (roleId.HasValue && user.AppUserRoles.All(x => x.UserRoleId != roleId.Value))
        {
            var role = new AppUserRole
            {
                UserRoleId = roleId.Value,
                AppUserId = userId
            };
            user.AppUserRoles.Add(role);
            _repository.Add(role);
        }

        if (user.UserContexts.All(context =>
                context.ContextType != normalizedType || context.ContextId != contextId))
        {
            var userContext = new UserContext(userId, normalizedType, contextId);
            user.UserContexts.Add(userContext);
            _repository.Add(userContext);
        }

        await _repository.SaveChangesAsync();

        var session = new UserModel(
            user.Id,
            user.Login,
            null,
            normalizedType,
            contextId,
            user.AppUserRoles.Select(x => x.UserRoleId).Distinct().ToList());

        await _cacheService.SetCachedDataAsync(
            new SessionKey(user.Id),
            session,
            TimeSpan.FromMinutes(10));

        return Result<UserModel>.Success(session);
    }
}
