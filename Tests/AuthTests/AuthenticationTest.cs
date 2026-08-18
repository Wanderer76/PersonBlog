using Authentication.Contract.Constants;
using Authentication.Contract.Events;
using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Authentication.Service.Service;
using Authentication.Service.Service.Implementation;
using Infrastructure.Services;
using MockQueryable.Moq;
using Moq;
using Shared;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace AuthTests;

public class AuthenticationTest
{
    private readonly Mock<IReadWriteRepository<IAuthEntity>> _repoMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ICurrentUserService> _userSessionMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly DefaultAuthService _authService;

    public AuthenticationTest()
    {
        _authService = new DefaultAuthService(
            _repoMock.Object,
            _tokenServiceMock.Object,
            _userSessionMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Authenticate_Fails_IfUserNotFound()
    {
        // Arrange
        var users = new List<AppUser>().BuildMockDbSet();
        _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);

        // Act
        var result = await _authService.Authenticate(new LoginPasswordModel("notfound", "pass"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Пользователь не найден", result.Errors.First().Message);
    }

    [Fact]
    public async Task Authenticate_Fails_IfPasswordInvalid()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Login = "test",
            Password = PasswordHasher.GetHash("correct"),
            AppUserRoles = new List<AppUserRole>(),
            UserContexts = new List<UserContext>()
        };
        var users = new List<AppUser> { user }.BuildMockDbSet();
        _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);
        _repoMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.Authenticate(new LoginPasswordModel("test", "wrong"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Неверный логин/пароль", result.Errors.First().Message);
    }

    [Fact]
    public async Task Authenticate_Succeeds_WithValidCredentials()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Login = "test",
            Password = PasswordHasher.GetHash("correct"),
            AppUserRoles = new List<AppUserRole>(),
            UserContexts = new List<UserContext>()
        };
        var users = new List<AppUser> { user }.BuildMockDbSet();
        _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);
        _repoMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _cacheServiceMock.Setup(x => x.SetCachedDataAsync(It.IsAny<ICacheKey>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.Authenticate(new LoginPasswordModel("test", "correct"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.AuthCode);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cacheServiceMock.Verify(x => x.SetCachedDataAsync(It.IsAny<SessionKey>(), It.IsAny<UserModel>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task Register_Fails_IfUserExists()
    {
        // Arrange
        var users = new List<AppUser> { new AppUser { Login = "existing" } }.BuildMockDbSet();
        _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);

        // Act
        var result = await _authService.Register(new RegisterRequest
        {
            Login = "existing",
            Password = "112312",
            PasswordConfirm = "112312",
            UserName = "test"
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Пользователь с таким логином уже существует", result.Errors.First().Message);
    }

    [Fact]
    public async Task Register_CreatesUser_Successfully()
    {
        // Arrange
        var userList = new List<AppUser>();
        var eventList = new List<AuthEvent>();
        var users = userList.BuildMockDbSet();
        _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);

        var regModel = new RegisterRequest
        {
            Login = "newuser",
            Password = "pass",
            PasswordConfirm = "pass",
            UserName = "test",
        };

        _repoMock.Setup(x => x.Add(It.IsAny<IAuthEntity>()))
            .Callback((IAuthEntity entity) =>
            {
                if (entity is AppUser user)
                {
                    userList.Add(user);
                }
                else if (entity is AuthEvent authEvent)
                {
                    eventList.Add(authEvent);
                }
            });

        _repoMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.Register(regModel);

        // Assert
        Assert.True(result.IsSuccess);
        _repoMock.Verify(x => x.Add(It.IsAny<AppUser>()), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
        Assert.Single(userList);
        Assert.Equal("newuser", userList[0].Login);
        var authEvent = Assert.Single(eventList);
        Assert.Equal(nameof(ProfileRegisterEvent), authEvent.EventType);
        Assert.Contains("\"Name\":\"test\"", authEvent.EventData);
        Assert.Contains("\"UserName\":\"newuser\"", authEvent.EventData);
    }

    [Fact]
    public async Task Logout_DeletesTokens_IfUserIdPresent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userSessionMock.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(new UserModel(userId, "test", null, Guid.Empty, []));

        var tokens = new List<Token>().BuildMockDbSet();
        _repoMock.Setup(x => x.Get<Token>()).Returns(tokens.Object);
        _cacheServiceMock.Setup(x => x.RemoveCachedDataAsync(It.IsAny<SessionKey>()))
            .Returns(Task.CompletedTask);

        // Act
        await _authService.Logout();

        // Assert
        _repoMock.Verify(x => x.Get<Token>(), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveCachedDataAsync(It.IsAny<SessionKey>()), Times.Once);
    }

    [Fact]
    public async Task Logout_DoesNothing_IfNoUserId()
    {
        // Arrange
        _userSessionMock.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(new UserModel(Guid.Empty, null, null, Guid.Empty, []));

        // Act
        await _authService.Logout();

        // Assert
        _repoMock.Verify(x => x.Get<Token>(), Times.Never);
        _cacheServiceMock.Verify(x => x.RemoveCachedDataAsync(It.IsAny<SessionKey>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_Fails_IfTokenInvalid()
    {
        // Arrange
        _tokenServiceMock.Setup(x => x.GetTokenRepresentation("t"))
            .Returns(Result<TokenModel>.Failure(new Error("Invalid token")));

        // Act
        var result = await _authService.Refresh("t");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Refresh_Fails_IfTokenNotRefresh()
    {
        // Arrange
        _tokenServiceMock.Setup(x => x.GetTokenRepresentation("t"))
            .Returns(Result<TokenModel>.Success(new TokenModel { Type = TokenTypes.Access, UserId = Guid.NewGuid() }));

        // Act
        var result = await _authService.Refresh("t");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Не верный тип токена", result.Errors.First().Message);
    }

    [Fact]
    public async Task Refresh_Succeeds_AndGeneratesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Login = "test",
            AppUserRoles = new List<AppUserRole>(),
            UserContexts = new List<UserContext>()
        };

        var users = new List<AppUser> { user }.BuildMockDbSet();
        _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);

        _tokenServiceMock.Setup(x => x.GetTokenRepresentation("t"))
            .Returns(Result<TokenModel>.Success(new TokenModel { Type = TokenTypes.Refresh, UserId = userId }));

        _tokenServiceMock.Setup(x => x.GenerateTokenAsync(user))
            .ReturnsAsync((new AuthResponse()));

        _tokenServiceMock.Setup(x => x.ClearUserToken("t"))
            .Returns(Task.CompletedTask);

        var tokens = new List<Token> { new Token { AppUserId = userId, TokenType = TokenTypes.Refresh } }.BuildMockDbSet();
        _repoMock.Setup(x => x.Get<Token>()).Returns(tokens.Object);

        _repoMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _cacheServiceMock.Setup(x => x.SetCachedDataAsync(It.IsAny<ICacheKey>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.Refresh("t");

        // Assert
        Assert.True(result.IsSuccess);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        _tokenServiceMock.Verify(x => x.ClearUserToken("t"), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsAnonymous_WhenTokenNull()
    {
        // Act
        var result = await _authService.GetCurrentUserAsync(null);

        // Assert
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsAnonymous);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser_WhenTokenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenData = new TokenModel
        {
            UserId = userId,
            Login = "test",
            ContextType = "conference",
            ContextId = Guid.NewGuid(),
            ExpiredAt = DateTimeService.Now().AddHours(1)
        };

        _tokenServiceMock.Setup(x => x.Validate("token")).Returns(true);
        _tokenServiceMock.Setup(x => x.GetTokenRepresentation("token")).Returns(Result<TokenModel>.Success(tokenData));

        var userRoles = new List<AppUserRole> { new() { UserRoleId = Roles.UserRoleId } }.BuildMockDbSet();
        _repoMock.Setup(x => x.Get<AppUserRole>()).Returns(userRoles.Object);

        // Act
        var result = await _authService.GetCurrentUserAsync("token");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsAnonymous);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(tokenData.ContextType, result.Value.ContextType);
        Assert.Equal(tokenData.ContextId, result.Value.ContextId);
    }
}
