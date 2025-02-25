using Infrastructure.Test;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Persistence;
using Profile.Service.Models;
using Profile.Service.Services.Implementation;
using Profile.Test.Mocks;
using Shared.Persistence;
using Xunit;

namespace Profile.Test
{
    [TestCaseOrderer(
        ordererTypeName: "Infrastructure.Test.PriorityOrderer",
        ordererAssemblyName: "Infrastructure.Test")]

    public class ProfileServiceTest : IClassFixture<ProfileDbSeedMock>
    {
        private readonly DefaultProfileService _profileService;
        private readonly ProfileDbSeedMock _mock;

        public ProfileServiceTest(ProfileDbSeedMock mock)
        {
            var repo = new DefaultRepository<ProfileDbContext,IProfileEntity>(mock.Context);

            _profileService = new DefaultProfileService(repo);
            _mock = mock;
        }

        [Fact]
        public async Task CreateTest()
        {
            var createModel = new ProfileCreateModel
            {
                UserId = _mock.UserIds.First(),
                Birthdate = DateTime.Now,
                Email = "",
                FirstName = "",
                LastName = "asd",
                SurName="asd",
                
            };
            var result = await _profileService.CreateProfileAsync(createModel);
            Assert.NotNull(result);
        }
    }
}
