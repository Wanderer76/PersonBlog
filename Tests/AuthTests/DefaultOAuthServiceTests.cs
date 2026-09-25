using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Authentication.Service.Service;
using Authentication.Service.Service.Implementation;
using Infrastructure.Services;
using Microsoft.AspNetCore.WebUtilities;
using MockQueryable.Moq;
using Moq;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using System.Reflection;

namespace AuthTests;

public sealed class DefaultOAuthServiceTests
{
    [Fact]
    public async Task GenerateAuthCodeAsync_RejectsUnsupportedResponseType()
    {
        var currentUser = new Mock<ICurrentUserService>();
        var service = CreateService([], currentUser.Object, Mock.Of<ICacheService>());

        var result = await service.GenerateAuthCodeAsync(
            "blog",
            "https://client.example/callback",
            "token",
            "state",
            "/return");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Key == "unsupported_response_type");
        currentUser.Verify(x => x.GetCurrentUserAsync(), Times.Never);
    }

    [Fact]
    public async Task GenerateAuthCodeAsync_RejectsUnregisteredRedirectUri()
    {
        var currentUser = new Mock<ICurrentUserService>();
        var service = CreateService(
            [CreateClient("blog", "https://client.example/callback")],
            currentUser.Object,
            Mock.Of<ICacheService>());

        var result = await service.GenerateAuthCodeAsync(
            "blog",
            "https://attacker.example/callback",
            "code",
            "state",
            "/return");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Key == "invalid_request");
        currentUser.Verify(x => x.GetCurrentUserAsync(), Times.Never);
    }

    [Fact]
    public async Task GenerateAuthCodeAsync_EncodesCallbackParametersAndBindsCodeToClient()
    {
        const string redirectUri = "https://client.example/callback?existing=value";
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(
            new UserModel(userId, "user", null, Guid.Empty, []));

        AuthCode? cachedCode = null;
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.SetCachedDataAsync(
                It.IsAny<ICacheKey>(),
                It.IsAny<AuthCode>(),
                It.IsAny<TimeSpan>()))
            .Callback<ICacheKey, AuthCode, TimeSpan>((_, value, _) => cachedCode = value)
            .Returns(Task.CompletedTask);

        var service = CreateService(
            [CreateClient("blog", redirectUri)],
            currentUser.Object,
            cache.Object);

        var result = await service.GenerateAuthCodeAsync(
            "blog",
            redirectUri,
            "code",
            "state&role=admin",
            "/posts?id=10&edit=true");

        Assert.True(result.IsSuccess);
        Assert.NotNull(cachedCode);
        Assert.Equal("blog", cachedCode.ClientId);
        Assert.Equal(userId, cachedCode.UserId);

        var query = QueryHelpers.ParseQuery(new Uri(result.Value.RedirectUrl).Query);
        Assert.Equal("value", query["existing"]);
        Assert.Equal(cachedCode.Code, query["code"]);
        Assert.Equal("state&role=admin", query["state"]);
        Assert.Equal("/posts?id=10&edit=true", query["returnUrl"]);
        Assert.False(query.ContainsKey("role"));
        Assert.False(query.ContainsKey("edit"));
    }

    [Fact]
    public async Task GenerateAuthCodeAsync_EncodesParametersForAuthenticationClient()
    {
        const string redirectUri = "https://client.example/callback";
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(UserModel.AnonymousUser());

        var service = CreateService(
            [
                CreateClient("blog", redirectUri),
                CreateClient("auth", "https://auth.example/sign-in?source=oauth")
            ],
            currentUser.Object,
            Mock.Of<ICacheService>());

        var result = await service.GenerateAuthCodeAsync(
            "blog",
            redirectUri,
            "code",
            "state&role=admin",
            "/posts?id=10&edit=true");

        Assert.True(result.IsSuccess);
        var query = QueryHelpers.ParseQuery(new Uri(result.Value.RedirectUrl).Query);
        Assert.Equal("oauth", query["source"]);
        Assert.Equal("blog", query["client_id"]);
        Assert.Equal(redirectUri, query["redirectUri"]);
        Assert.Equal("state&role=admin", query["state"]);
        Assert.Equal("/posts?id=10&edit=true", query["returnUrl"]);
        Assert.False(query.ContainsKey("role"));
        Assert.False(query.ContainsKey("edit"));
    }

    private static DefaultOAuthService CreateService(
        IReadOnlyCollection<Client> clients,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        var repository = new Mock<IReadWriteRepository<IAuthEntity>>();
        repository.Setup(x => x.Get<Client>())
            .Returns(clients.ToList().BuildMockDbSet().Object);

        return new DefaultOAuthService(
            repository.Object,
            currentUserService,
            cacheService,
            Mock.Of<ITokenService>());
    }

    private static Client CreateClient(string clientId, string redirectUri)
    {
        var constructor = typeof(Client).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null)!;
        var client = (Client)constructor.Invoke(null);
        typeof(Client).GetProperty(nameof(Client.ClientId))!.SetValue(client, clientId);
        typeof(Client).GetProperty(nameof(Client.RedirectUri))!.SetValue(client, redirectUri);
        return client;
    }
}
