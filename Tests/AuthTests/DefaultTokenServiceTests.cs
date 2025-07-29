using Authentication.Domain.Entities;
using Authentication.Service.Service.Implementation;
using MockQueryable.Moq;
using Moq;
using Shared;
using Shared.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthTests
{
    public class DefaultTokenServiceTests
    {
        private readonly Mock<IReadWriteRepository<IAuthEntity>> _repoMock = new();
        private readonly DefaultTokenService _service;

        public DefaultTokenServiceTests()
        {
            _service = new DefaultTokenService(_repoMock.Object);
        }

        [Fact]
        public void Validate_ReturnsFalse_IfTokenRepresentationIsNull()
        {

            var result = _service.Validate("invalid");

            Assert.False(result);
        }

        [Fact]
        public void Validate_ReturnsFalse_IfExpired()
        {

            var result = _service.Validate("expired");

            Assert.False(result);
        }

        [Fact]
        public void Validate_ReturnsTrue_IfValid()
        {
            var result = _service.Validate("valid");

            Assert.True(result);
        }

        [Fact]
        public async Task GenerateTokenAsync_Should_ReturnAuthResponse_AndSaveTokens()
        {
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Login = "test",
                AppUserRoles = new List<AppUserRole> { new() { UserRoleId = roleId } }
            };

            var profiles = new List<AppProfile> {
            new AppProfile { UserId = userId, BlogId = Guid.NewGuid() }
        }.BuildMockDbSet();

            _repoMock.Setup(x => x.Get<AppProfile>()).Returns(profiles.Object);

            var result = await _service.GenerateTokenAsync(user);

            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("refresh-token", result.RefreshToken);

            _repoMock.Verify(x => x.Add(It.Is<Token>(t => t.TokenType == TokenTypes.Access)), Times.Once);
            _repoMock.Verify(x => x.Add(It.Is<Token>(t => t.TokenType == TokenTypes.Refresh)), Times.Once);
        }

        [Fact]
        public async Task ClearUserToken_Should_Delete_Tokens_ByUserId()
        {
            var userId = Guid.NewGuid();


            var tokens = new List<Token>().BuildMockDbSet();
            _repoMock.Setup(x => x.Get<Token>()).Returns(tokens.Object);

            await _service.ClearUserToken("tok");

            _repoMock.Verify(x => x.Get<Token>(), Times.Once);
        }

        [Fact]
        public void GetTokenRepresentation_Returns_JwtUtils_ParsedModel()
        {
            var expected = new TokenModel { Id = Guid.NewGuid() };

            var result = _service.GetTokenRepresentation("t");

            Assert.Equal(expected.Id, result.Id);
        }

        [Fact]
        public void GenerateToken_Returns_JwtTokens_FromClaims()
        {
            var roleId = Guid.NewGuid();
            var blogId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var tokenId = Guid.NewGuid();

            var user = new AppUser
            {
                Id = userId,
                Login = "user",
                AppUserRoles = new List<AppUserRole> { new() { UserRoleId = roleId } }
            };

            var claims = new Dictionary<string, string>
        {
            { AppClaimTypes.Id, tokenId.ToString() },
            { AppClaimTypes.BlogId, blogId.ToString() },
            { AppClaimTypes.RoleId, roleId.ToString() },
            { AppClaimTypes.UserId, userId.ToString() },
            { AppClaimTypes.Login, "user" },
        };

            var result = _service.GenerateToken(user, claims);

            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("refresh-token", result.RefreshToken);
        }
    }

}
