using Authentication.Domain.Entities;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared;
using Shared.Services;

namespace AuthTests
{
    public class HttpContextUserServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly HttpContextUserService _service;
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid BlogId = Guid.NewGuid();
        private const string Login = "testuser";
        private const string RoleId = "00000000-0000-0000-0000-000000000001";

        private static string ValidJwtToken =>
            JwtUtils.GetJwtTokens(
                new TokenModel
                {
                    Id = Guid.NewGuid(),
                    UserId = UserId,
                    RoleId = Guid.Parse(RoleId),
                    BlogId = BlogId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiredAt = DateTimeOffset.UtcNow.AddMinutes(10),
                    Login = Login,
                    Type = TokenTypes.Access
                },
                 new TokenModel
                 {
                     Id = Guid.NewGuid(),
                     UserId = UserId,
                     RoleId = Guid.Parse(RoleId),
                     BlogId = BlogId,
                     CreatedAt = DateTimeOffset.UtcNow,
                     ExpiredAt = DateTimeOffset.UtcNow.AddMinutes(10),
                     Login = Login,
                     Type = TokenTypes.Refresh
                 }
            ).access;

        public HttpContextUserServiceTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _service = new HttpContextUserService(_httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task GetCurrentUserAsync_ReturnsAnonymous_WhenNoAuthorizationHeader()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            // Act
            var result = await _service.GetCurrentUserAsync();

            // Assert
            Assert.True(result.IsAnonymous);
        }

        [Fact]
        public async Task GetCurrentUserAsync_ReturnsAnonymous_WhenHeaderIsInvalid()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Authorization = "InvalidHeader";
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            // Act
            var result = await _service.GetCurrentUserAsync();

            // Assert
            Assert.True(result.IsAnonymous);
        }

        [Fact]
        public async Task GetCurrentUserAsync_ReturnsUser_WhenTokenIsValid()
        {
            // Arrange
            var token = ValidJwtToken; // см. ниже — валидный токен

            var context = new DefaultHttpContext();
            context.Request.Headers.Authorization = $"Bearer {token}";
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            // Act
            var result = await _service.GetCurrentUserAsync();

            // Assert
            Assert.Equal(UserId, result.UserId);
            Assert.Equal(Login, result.UserName);
            Assert.Equal(BlogId, result.BlogId);
            Assert.False(result.IsAnonymous);
        }

        [Fact]
        public async Task GetCurrentUserAsync_UsesCachedUserModel()
        {
            // Arrange
            var token = ValidJwtToken;
            var context = new DefaultHttpContext();
            context.Request.Headers.Authorization = $"Bearer {token}";
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            var firstCall = await _service.GetCurrentUserAsync();
            var secondCall = await _service.GetCurrentUserAsync();
            context.Request.Headers.Authorization = $"Invalid";
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
            var thirdCall = await _service.GetCurrentUserAsync();

            // Assert
            Assert.Same(firstCall, secondCall); // кэш используется
            Assert.Same(firstCall, thirdCall); // кэш используется
        }
    }

}
