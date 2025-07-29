using Authentication.Domain.Entities;
using Authentication.Service.Models;
using Authentication.Service.Service;
using Authentication.Service.Service.Implementation;
using Authentication.Test.Mocks;
using AuthenticationApplication.Models;
using Common;
using Infrastructure.Services;
using MockQueryable.Moq;
using Moq;
using Shared;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using Xunit;

namespace AuthTests
{
    [TestCaseOrderer(
    ordererTypeName: "Infrastructure.Test.PriorityOrderer",
    ordererAssemblyName: "Infrastructure.Test")]
    public class AuthenticationTest : IClassFixture<AuthDbSeedMock>
    {
        private readonly Mock<IReadWriteRepository<IAuthEntity>> _repoMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<ICurrentUserService> _userSessionMock = new();
        private readonly AuthDbSeedMock _dbSeedMock;
        private readonly DefaultAuthService _authService;
        public AuthenticationTest()
        {
            //_profileApiClient = new Mock<IProfileApiAsyncClient>();
            //_profileApiClient.Setup(x => x.CreateProfileAsync(It.IsAny<ProfileCreateRequest>())).ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            //_dbSeedMock = dbSeedMock;
            //_context = new DefaultRepository<AuthenticationDbContext, IAuthEntity>(AuthDbSeedMock.Context);
            //_tokenService = new DefaultTokenService(_context);
            _authService = new DefaultAuthService(_repoMock.Object, _tokenServiceMock.Object, _userSessionMock.Object);
        }
        [Fact]
        public async Task Authenticate_Fails_IfUserNotFound()
        {
            var users = new List<AppUser>().BuildMockDbSet();
            _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);

            var result = await _authService.Authenticate(new LoginPasswordModel("notfound", "pass"));
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task Authenticate_Fails_IfPasswordInvalid()
        {
            var user = new AppUser { Login = "test", Password = "hash" };
            var users = new List<AppUser> { user }.BuildMockDbSet();

            _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);

            var result = await _authService.Authenticate(new LoginPasswordModel("test", "wrong"));

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task Authenticate_Succeeds_WithValidCredentials()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Login = "test", Password = PasswordHasher.GetHash("correct"), AppUserRoles = new() };
            var users = new List<AppUser> { user }.BuildMockDbSet();

            _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);

            _tokenServiceMock.Setup(x => x.GenerateTokenAsync(user)).ReturnsAsync(new AuthResponse());

            var result = await _authService.Authenticate(new LoginPasswordModel("test", "correct"));

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Register_Fails_IfUserExists()
        {
            var users = new List<AppUser> { new AppUser { Login = "existing" } }.BuildMockDbSet();
            _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);

            var result = await _authService.Register(new RegisterModel { Login = "existing", Email = "" });

            Assert.False(result.IsSuccess);
        }

        /// <summary>
        /// не проходит из-за мока, нужен inmemoryDbset
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Register_CreatesUser_AndAuthenticates()
        {
            var users = new List<AppUser>().BuildMockDbSet();
            _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);
            var regModel = new RegisterModel
            {
                Login = "newuser",
                Password = "pass",
                Name = "N",
                Surname = "S",
                Lastname = "L",
                Birthdate = new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Email = "test@mail.com"
            };

            _tokenServiceMock.Setup(x => x.GenerateTokenAsync(It.IsAny<AppUser>()))
                             .ReturnsAsync(new AuthResponse());

            var result = await _authService.Register(regModel);

            Assert.True(result.IsSuccess);
            _repoMock.Verify(x => x.Add(It.IsAny<AppUser>()), Times.Once);
            _repoMock.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Logout_DeletesTokens_IfUserIdPresent()
        {
            var userId = Guid.NewGuid();
            _userSessionMock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new Shared.Models.UserModel { UserId = userId });

            var tokens = new List<Token>().BuildMockDbSet();
            _repoMock.Setup(x => x.Get<Token>()).Returns(tokens.Object);

            await _authService.Logout();

            _repoMock.Verify(x => x.Get<Token>(), Times.Once);
        }

        [Fact]
        public async Task Logout_DoesNothing_IfNoUserId()
        {
            _userSessionMock.Setup(x => x.GetCurrentUserAsync())
                            .ReturnsAsync(new UserModel { UserId = null });

            await _authService.Logout();

            _repoMock.Verify(x => x.Get<Token>(), Times.Never);
        }

        [Fact]
        public async Task Refresh_Fails_IfTokenNotRefresh()
        {
            _tokenServiceMock.Setup(x => x.GetTokenRepresentation("t"))
                             .Returns(new TokenModel { Type = TokenTypes.Access });

            var result = await _authService.Refresh("t");

            Assert.False(result.IsSuccess);
            Assert.Equal("Не верный тип токена", result.Error!.Message);
        }

        [Fact]
        public async Task Refresh_Succeeds_AndGeneratesToken()
        {
            var userId = Guid.NewGuid();
            var user = new AppUser { Id = userId, AppUserRoles = new() };

            var users = new List<AppUser> { user }.BuildMockDbSet();
            _repoMock.Setup(x => x.Get<AppUser>()).Returns(users.Object);

            _tokenServiceMock.Setup(x => x.GetTokenRepresentation("t"))
                             .Returns(new TokenModel { Type = TokenTypes.Refresh, UserId = userId });

            _tokenServiceMock.Setup(x => x.GenerateTokenAsync(user))
                             .ReturnsAsync(new AuthResponse());

            var result = await _authService.Refresh("t");

            Assert.True(result.IsSuccess);
            _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ValidateToken_Returns_Correctly(bool isValid)
        {
            _tokenServiceMock.Setup(x => x.Validate("token")).Returns(isValid);

            var result = await _authService.ValidateToken("token");

            Assert.Equal(isValid, result);

            if (!isValid)
                _tokenServiceMock.Verify(x => x.ClearUserToken("token"), Times.Once);
            else
                _tokenServiceMock.Verify(x => x.ClearUserToken(It.IsAny<string>()), Times.Never);
        }
    }
}
