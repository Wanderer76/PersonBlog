using Authentication.Contract.Constants;
using Authentication.Domain.Entities;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;

namespace Authentication.Service.Service.Implementation;

internal sealed class DefaultBlogUserProvisioningService(
    IReadWriteRepository<IAuthEntity> repository,
    ICacheService cacheService) : IBlogUserProvisioningService
{
    public async Task<UserModel> ProvisionBlogAsync(Guid userId, Guid blogId)
    {
        var user = await repository.Get<AppUser>()
            .Include(x => x.AppUserRoles)
            .Include(x => x.UserContexts)
            .FirstAsync(x => x.Id == userId);

        var existingBlogContext = user.UserContexts
            .FirstOrDefault(x => x.ContextType == UserContextType.Blog);

        if (existingBlogContext is not null && existingBlogContext.ContextId != blogId)
        {
            throw new InvalidOperationException("Пользователь уже связан с другим блогом");
        }

        if (!user.AppUserRoles.Any(x => x.UserRoleId == Roles.BloggerRoleId))
        {
            var blogRole = new AppUserRole
            {
                UserRoleId = Roles.BloggerRoleId,
                AppUserId = userId
            };
            user.AppUserRoles.Add(blogRole);
            repository.Add(blogRole);
        }

        if (existingBlogContext is null)
        {
            var blogUserContext = new UserContext(userId, UserContextType.Blog, blogId);
            user.UserContexts.Add(blogUserContext);
            repository.Add(blogUserContext);
        }

        await repository.SaveChangesAsync();

        var session = new UserModel(
            user.Id,
            user.Login,
            null,
            blogId,
            user.AppUserRoles.Select(x => x.UserRoleId).Distinct().ToList());

        await cacheService.SetCachedDataAsync(
            new SessionKey(user.Id),
            session,
            TimeSpan.FromMinutes(10));

        var token = await repository.Get<Token>()
            .Where(x => x.AppUserId == userId)
            .Where(x => x.TokenType == TokenTypes.Access)
            .FirstOrDefaultAsync();

        if (token is not null)
        {
            await cacheService.SetCachedDataAsync(
                new BlacklistTokenCacheKey(token.Id),
                token.ToTokenModel(user),
                token.ExpiredAt - token.CreatedAt);
        }

        return session;
    }
}
