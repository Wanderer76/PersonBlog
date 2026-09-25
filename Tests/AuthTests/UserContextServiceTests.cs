using Authentication.Contract.Constants;
using Authentication.Domain.Entities;
using Authentication.Service.Service;
using Authentication.Service.Service.Implementation;
using AuthenticationApplication.Controllers;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System.Security.Claims;
using System.Text.Json;

namespace AuthTests;

public sealed class UserContextServiceTests
{
    [Fact]
    public async Task ActivateContext_ForbidsUserIdDifferentFromAuthenticatedUser()
    {
        var authenticatedUserId = Guid.NewGuid();
        var contextService = new Mock<IUserContextService>();
        var controller = CreateController(authenticatedUserId, contextService.Object);
        var request = new Authentication.Contract.Models.ActivateUserContextRequest(
            Guid.NewGuid(),
            "conference",
            Guid.NewGuid());

        var response = await controller.ActivateContext(request, CancellationToken.None);

        Assert.IsType<ForbidResult>(response.Result);
        contextService.Verify(x => x.GrantAndActivateAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void UserModel_RoundTripsGenericContext()
    {
        var model = new UserModel(
            Guid.NewGuid(),
            "user",
            null,
            "conference",
            Guid.NewGuid(),
            [Guid.NewGuid()]);

        var json = JsonSerializer.Serialize(model);
        var deserialized = JsonSerializer.Deserialize<UserModel>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(model.UserId, deserialized.UserId);
        Assert.Equal(model.ContextType, deserialized.ContextType);
        Assert.Equal(model.ContextId, deserialized.ContextId);
        Assert.Equal(model.Roles, deserialized.Roles);
    }

    [Fact]
    public async Task GrantAndActivateAsync_ReusesPersistedContext()
    {
        var userId = Guid.NewGuid();
        var contextId = Guid.NewGuid();
        var user = CreateUser(userId, new UserContext(userId, "conference", contextId));
        var (service, repository, getSession) = CreateService(user);

        var result = await service.GrantAndActivateAsync(userId, " CONFERENCE ", contextId);

        Assert.True(result.IsSuccess);
        Assert.Equal("conference", result.Value.ContextType);
        Assert.Equal(contextId, result.Value.ContextId);
        Assert.Same(result.Value, getSession());
        repository.Verify(x => x.Add(It.IsAny<UserContext>()), Times.Never);
    }

    [Fact]
    public async Task GrantAndActivateAsync_PersistsNewContextAndRole()
    {
        var userId = Guid.NewGuid();
        var contextId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = CreateUser(userId);
        var (service, repository, getSession) = CreateService(user);

        var result = await service.GrantAndActivateAsync(userId, "conference", contextId, roleId);

        Assert.True(result.IsSuccess);
        Assert.Equal("conference", result.Value.ContextType);
        Assert.Equal(contextId, result.Value.ContextId);
        Assert.Contains(roleId, result.Value.Roles);
        Assert.Same(result.Value, getSession());
        repository.Verify(x => x.Add(It.Is<UserContext>(context =>
            context.ContextType == "conference" && context.ContextId == contextId)));
        repository.Verify(x => x.Add(It.Is<AppUserRole>(role => role.UserRoleId == roleId)));
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GrantAndActivateAsync_ReturnsFailure_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var (service, repository, getSession) = CreateService(CreateUser(Guid.NewGuid()));

        var result = await service.GrantAndActivateAsync(userId, "conference", Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Key == "NotFound");
        Assert.Null(getSession());
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static AppUser CreateUser(Guid userId, params UserContext[] contexts) => new()
    {
        Id = userId,
        Login = "user",
        AppUserRoles =
        [
            new AppUserRole { AppUserId = userId, UserRoleId = Roles.UserRoleId }
        ],
        UserContexts = contexts.ToList()
    };

    private static (
        IUserContextService Service,
        Mock<IReadWriteRepository<IAuthEntity>> Repository,
        Func<UserModel?> GetSession) CreateService(AppUser user)
    {
        var repository = new Mock<IReadWriteRepository<IAuthEntity>>();
        repository.Setup(x => x.Get<AppUser>())
            .Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        repository.Setup(x => x.Get<UserContext>())
            .Returns(user.UserContexts.BuildMockDbSet().Object);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        UserModel? session = null;
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.SetCachedDataAsync(
                It.Is<ICacheKey>(key => key is SessionKey),
                It.IsAny<UserModel>(),
                It.IsAny<TimeSpan>()))
            .Callback<ICacheKey, UserModel, TimeSpan>((_, value, _) => session = value)
            .Returns(Task.CompletedTask);

        return (
            new DefaultUserContextService(repository.Object, cache.Object),
            repository,
            () => session);
    }

    private static AuthController CreateController(
        Guid authenticatedUserId,
        IUserContextService contextService)
    {
        var identity = new ClaimsIdentity(
            [new Claim(AppClaimTypes.UserId, authenticatedUserId.ToString())],
            "Test");
        var controller = new AuthController(
            Mock.Of<ILogger<AuthController>>(),
            Mock.Of<IAuthService>(),
            contextService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };

        return controller;
    }
}
