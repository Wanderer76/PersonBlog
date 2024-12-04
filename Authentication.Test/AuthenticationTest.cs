using Authentication.Domain.Entities;
using Authentication.Peristence;
using Authentication.Service.Service.Implementation;
using Authentication.Test.Mocks;
using AuthenticationApplication.Models;
using AuthenticationApplication.Models.Requests;
using AuthenticationApplication.Service;
using AuthenticationApplication.Service.ApiClient;
using Infrastructure.Test;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Persistence;
using Xunit;

namespace Authentication.Test
{
    [TestCaseOrderer(
    ordererTypeName: "Infrastructure.Test.PriorityOrderer",
    ordererAssemblyName: "Infrastructure.Test")]
    public class AuthenticationTest : IClassFixture<AuthDbSeedMock>
    {
        private readonly DefaultTokenService _tokenService;
        private readonly DefaultRepository<AuthenticationDbContext, IAuthEntity> _context;
        private readonly Mock<IProfileApiAsyncClient> _profileApiClient;
        private readonly AuthDbSeedMock _dbSeedMock;
        private readonly DefaultAuthService _authService;
        public AuthenticationTest(AuthDbSeedMock dbSeedMock)
        {
            _profileApiClient = new Mock<IProfileApiAsyncClient>();
            _profileApiClient.Setup(x => x.CreateProfileAsync(It.IsAny<ProfileCreateRequest>())).ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            _dbSeedMock = dbSeedMock;
            _context = new DefaultRepository<AuthenticationDbContext, IAuthEntity>(AuthDbSeedMock.Context);
            _tokenService = new DefaultTokenService(_context);
            _authService = new DefaultAuthService(_context, _profileApiClient.Object, _tokenService);
        }

        [Fact, TestPriority(1)]
        public void RegistrationTest()
        {

            var registerModel = new RegisterModel
            {
                Name = "admin",
                Password = "admin",
                Birthdate = DateTime.Now,
                Login = "admin",
                PasswordConfirm = "admin",
                //UserRoleIds = new[] { Guid.Parse("57a2b99b-b6ee-4c98-a1f0-b18fe96dae60") },
                Surname = "admin",
            };

            var result = _authService.Register(registerModel).Result;

            var dbUser = _context.Get<AppUser>()
                .Include(x => x.AppUserRoles)
                .Where(x => x.Login == "admin")
                .FirstOrDefault();

            Assert.NotNull(dbUser);
            Assert.Single(dbUser.AppUserRoles);
        }

        [Fact, TestPriority(2)]
        public void AuthenticationTest1()
        {
            var loginModel = new LoginModel
            {
                Login = "admin",
                Password = "admin"
            };

            var authResult = _authService.Authenticate(loginModel).Result;

            Assert.NotNull(authResult);
            Assert.NotNull(authResult.RefreshToken);
            Assert.NotNull(authResult.AccessToken);

            var accessRepresentation = _tokenService.GetTokenRepresentaion(authResult.AccessToken);
            Assert.NotNull(accessRepresentation);
            Assert.True(accessRepresentation.ExpiredAt > DateTimeOffset.UtcNow);
            Assert.True(accessRepresentation.RoleId != Guid.Empty);

            var refreshRepresentation = _tokenService.GetTokenRepresentaion(authResult.RefreshToken);
            Assert.NotNull(refreshRepresentation);
            Assert.True(refreshRepresentation.ExpiredAt > DateTime.UtcNow);
            Assert.True(refreshRepresentation.RoleId != Guid.Empty);

            Assert.True(_tokenService.Validate(authResult.AccessToken));
            Assert.True(_tokenService.Validate(authResult.RefreshToken));

        }

        [Fact, TestPriority(2)]
        public void AuthenticationTest2()
        {
            var loginModel = new LoginModel
            {
                Login = "admin1",
                Password = "admin1"
            };

            var authResult = _authService.Authenticate(loginModel).Result;

            Assert.NotNull(authResult);
            Assert.NotNull(authResult.RefreshToken);
            Assert.NotNull(authResult.AccessToken);

            var accessRepresentation = _tokenService.GetTokenRepresentaion(authResult.AccessToken);
            Assert.NotNull(accessRepresentation);
            Assert.True(accessRepresentation.ExpiredAt > DateTimeOffset.UtcNow);
            Assert.True(accessRepresentation.RoleId != Guid.Empty);

            var refreshRepresentation = _tokenService.GetTokenRepresentaion(authResult.RefreshToken);
            Assert.NotNull(refreshRepresentation);
            Assert.True(refreshRepresentation.ExpiredAt > DateTime.UtcNow);
            Assert.True(refreshRepresentation.RoleId != Guid.Empty);

            Assert.True(_tokenService.Validate(authResult.AccessToken));
            Assert.True(_tokenService.Validate(authResult.RefreshToken));

        }
    }
}
