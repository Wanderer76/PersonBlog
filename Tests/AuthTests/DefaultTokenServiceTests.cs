//using Authentication.Domain.Entities;
//using Authentication.Service.Service.Implementation;
//using Microsoft.EntityFrameworkCore;
//using MockQueryable.Moq;
//using Moq;
//using Shared.Persistence;
//using Shared.Services;

//namespace AuthTests
//{
//    public class DefaultTokenServiceTests
//    {
//        private static readonly Guid UserId = Guid.NewGuid();
//        private static readonly Guid RoleId = Guid.NewGuid();
//        private static readonly Guid BlogId = Guid.NewGuid();
//        private static readonly Guid TokenId = Guid.NewGuid();

//        // Валидный JWT
//        private string ValidAccessToken, ValidRefreshToken;

//        // Просроченный JWT
//        private string ExpiredAccessToken, ExpiredRefreshToken;

//        private const string Login = "testuser";
//        public DefaultTokenServiceTests()
//        {
//            (ValidAccessToken, ValidRefreshToken) = JwtUtils.GetJwtTokens(new Shared.TokenModel
//            {
//                UserId = UserId,
//                RoleId = RoleId,
//                BlogId = BlogId,
//                CreatedAt = DateTime.UtcNow,
//                ExpiredAt = DateTime.UtcNow.AddMinutes(10),
//                Id = TokenId,
//                Login = Login,
//                Type = TokenTypes.Access
//            },
//           new Shared.TokenModel
//           {
//               UserId = UserId,
//               RoleId = RoleId,
//               BlogId = BlogId,
//               CreatedAt = DateTime.UtcNow,
//               ExpiredAt = DateTime.UtcNow.AddMinutes(100),
//               Id = TokenId,
//               Login = Login,
//               Type = TokenTypes.Refresh
//           });

//            (ExpiredAccessToken, ExpiredRefreshToken) = JwtUtils.GetJwtTokens(new Shared.TokenModel
//            {
//                UserId = UserId,
//                RoleId = RoleId,
//                BlogId = BlogId,
//                CreatedAt = DateTime.UtcNow.AddMinutes(-100),
//                ExpiredAt = DateTime.UtcNow.AddMinutes(-10),
//                Id = TokenId,
//                Login = Login,
//                Type = TokenTypes.Access
//            },
//          new Shared.TokenModel
//          {
//              UserId = UserId,
//              RoleId = RoleId,
//              BlogId = BlogId,
//              CreatedAt = DateTime.UtcNow.AddMinutes(-100),
//              ExpiredAt = DateTime.UtcNow.AddMinutes(-100),
//              Id = TokenId,
//              Login = Login,
//              Type = TokenTypes.Refresh
//          });

//        }
//        [Fact]
//        public async Task GenerateTokenAsync_CreatesTokens_AndReturnsAuthResponse()
//        {
//            var repoMock = new Mock<IReadWriteRepository<IAuthEntity>>();
//            var appProfiles = new List<AppProfile> {
//        new AppProfile { UserId = UserId, BlogId = BlogId }
//    }.BuildMockDbSet();

//            repoMock.Setup(x => x.Get<AppProfile>()).Returns(appProfiles.Object);

//            var storedTokens = new List<Token>();
//            repoMock.Setup(x => x.Add(It.IsAny<IAuthEntity>()))
//                    .Callback<IAuthEntity>(e =>
//                    {
//                        if (e is Token t) storedTokens.Add(t);
//                    });

//            var service = new DefaultTokenService(repoMock.Object);

//            var user = new AppUser
//            {
//                Id = UserId,
//                Login = Login,
//                AppUserRoles = new List<AppUserRole> {
//            new AppUserRole { UserRoleId = RoleId }
//        }
//            };

//            var result = await service.GenerateTokenAsync(user);

//            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
//            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
//            Assert.Equal(2, storedTokens.Count); // Access + Refresh
//        }

//        [Fact]
//        public void GenerateToken_CreatesTokens_FromClaims()
//        {
//            var repoMock = new Mock<IReadWriteRepository<IAuthEntity>>();
//            var service = new DefaultTokenService(repoMock.Object);

//            var claims = new Dictionary<string, string>
//            {
//                [AppClaimTypes.Id] = TokenId.ToString(),
//                [AppClaimTypes.UserId] = UserId.ToString(),
//                [AppClaimTypes.RoleId] = RoleId.ToString(),
//                [AppClaimTypes.BlogId] = BlogId.ToString(),
//                [AppClaimTypes.Login] = Login
//            };

//            var user = new AppUser
//            {
//                Id = UserId,
//                Login = Login,
//                AppUserRoles = new List<AppUserRole> {
//            new AppUserRole { UserRoleId = RoleId }
//        }
//            };

//            var result = service.GenerateToken(user, claims);

//            Assert.NotNull(result.AccessToken);
//            Assert.NotNull(result.RefreshToken);
//        }

//        [Fact]
//        public void Validate_ReturnsTrue_ForValidToken()
//        {
//            var service = new DefaultTokenService(Mock.Of<IReadWriteRepository<IAuthEntity>>());

//            var result = service.Validate(ValidAccessToken);

//            Assert.True(result);
//        }

//        [Fact]
//        public void Validate_ReturnsFalse_ForExpiredToken()
//        {
//            var service = new DefaultTokenService(Mock.Of<IReadWriteRepository<IAuthEntity>>());

//            var result = service.Validate(ExpiredAccessToken);

//            Assert.False(result);
//        }

//        [Fact]
//        public void GetTokenRepresentation_ParsesValidJwt()
//        {
//            var service = new DefaultTokenService(Mock.Of<IReadWriteRepository<IAuthEntity>>());

//            var model = service.GetTokenRepresentation(ValidAccessToken);

//            Assert.Equal(UserId, model.UserId);
//            Assert.Equal(RoleId, model.RoleId);
//            Assert.Equal(BlogId, model.BlogId);
//            Assert.Equal(TokenTypes.Access, model.Type);
//            Assert.Equal(Login, model.Login);
//        }

//        [Fact]
//        public async Task ClearUserToken_CallsGetOnRepository()
//        {
//            // Arrange
//            // Arrange
//            var repoMock = new Mock<IReadWriteRepository<IAuthEntity>>();

//            var tokens = new List<Token>
//    {
//        new Token { AppUserId = UserId }
//    };

//            var mockDbSet = tokens.BuildMockDbSet();

//            // Setup Get<Token>() to return the mocked set
//            repoMock.Setup(x => x.Get<Token>()).Returns(mockDbSet.Object);

//            // 👇 Здесь мы НЕ вызываем ExecuteDeleteAsync напрямую, он вызовется из тестируемого кода
//            // поэтому не нужно делать Setup — ты просто проверяешь, что код работает

//            var service = new DefaultTokenService(repoMock.Object);

//            // Act
//            await service.ClearUserToken(ValidAccessToken);

//            // Assert
//            repoMock.Verify(x => x.Get<Token>(), Times.Once);
//        }
//    }

//}
