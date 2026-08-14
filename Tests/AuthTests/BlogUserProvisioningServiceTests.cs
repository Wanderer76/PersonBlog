using Authentication.Contract.Constants;
using Authentication.Domain.Entities;
using Authentication.Service.Service.Implementation;
using Infrastructure.Services;
using MockQueryable.Moq;
using Moq;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace AuthTests;

public sealed class BlogUserProvisioningServiceTests
{
    [Fact]
    public async Task ProvisionBlogAsync_RefreshesSession_WhenBlogContextAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var blogId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Login = "blogger",
            AppUserRoles =
            [
                new AppUserRole { AppUserId = userId, UserRoleId = Roles.UserRoleId },
                new AppUserRole { AppUserId = userId, UserRoleId = Roles.BloggerRoleId }
            ],
            UserContexts = [new UserContext(userId, UserContextType.Blog, blogId)]
        };

        var repository = new Mock<IReadWriteRepository<IAuthEntity>>();
        repository.Setup(x => x.Get<AppUser>())
            .Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        repository.Setup(x => x.Get<Token>())
            .Returns(new List<Token>().BuildMockDbSet().Object);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(0);

        UserModel? cachedSession = null;
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.SetCachedDataAsync(
                It.Is<ICacheKey>(key => key is SessionKey),
                It.IsAny<UserModel>(),
                It.IsAny<TimeSpan>()))
            .Callback<ICacheKey, UserModel, TimeSpan>((_, session, _) => cachedSession = session)
            .Returns(Task.CompletedTask);

        var service = new DefaultBlogUserProvisioningService(repository.Object, cache.Object);

        var result = await service.ProvisionBlogAsync(userId, blogId);

        Assert.Same(result, cachedSession);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(blogId, result.BlogId);
        Assert.Contains(Roles.BloggerRoleId, result.Roles);
    }
}
