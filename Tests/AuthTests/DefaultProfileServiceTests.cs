using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces.Models.Profile;
using Authentication.Service.Service.Implementation;
using MockQueryable.Moq;
using Moq;
using Shared.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthTests
{
    public class DefaultProfileServiceTests
    {
        private readonly Mock<IReadWriteRepository<IAuthEntity>> _repoMock = new();
        private readonly DefaultProfileService _service;

        public DefaultProfileServiceTests()
        {
            _service = new DefaultProfileService(_repoMock.Object);
        }

        [Fact]
        public async Task CreateProfileAsync_Should_Throw_If_Profile_Exists()
        {
            var userId = Guid.NewGuid();
            var existingProfiles = new List<AppProfile>
        {
            new AppProfile { UserId = userId, IsDeleted = true }
        }.BuildMockDbSet();

            _repoMock.Setup(x => x.Get<AppProfile>()).Returns(existingProfiles.Object);

            var model = new ProfileCreateModel { UserId = userId };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateProfileAsync(model));
        }

        [Fact]
        public async Task CreateProfileAsync_Should_Create_If_NotExists()
        {
            var userId = Guid.NewGuid();

            var emptyProfiles = new List<AppProfile>().BuildMockDbSet();
            _repoMock.Setup(x => x.Get<AppProfile>()).Returns(emptyProfiles.Object);

            var model = new ProfileCreateModel
            {
                UserId = userId,
                FirstName = "A",
                SurName = "B",
                LastName = "C",
                Birthdate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Email = "test@mail.com"
            };

            var result = await _service.CreateProfileAsync(model);

            _repoMock.Verify(x => x.Add(It.IsAny<AppProfile>()), Times.Once);
            _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);

            Assert.Equal(userId, result.UserId);
            Assert.Equal("test@mail.com", result.Email);
        }

        [Fact]
        public async Task DeleteProfileByUserIdAsync_Should_Set_IsDeleted()
        {
            var userId = Guid.NewGuid();
            var profile = new AppProfile { UserId = userId, IsDeleted = false };

            var profileSet = new List<AppProfile> { profile }.BuildMockDbSet();

            _repoMock.Setup(x => x.Get<AppProfile>()).Returns(profileSet.Object);

            await _service.DeleteProfileByUserIdAsync(userId);

            Assert.True(profile.IsDeleted);
            _repoMock.Verify(x => x.Attach(profile), Times.Once);
            _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetProfileByUserIdAsync_Should_Return_Correct_Profile()
        {
            var userId = Guid.NewGuid();
            var profile = new AppProfile { UserId = userId, Email = "p@mail.com" };
            var set = new List<AppProfile> { profile }.BuildMockDbSet();

            _repoMock.Setup(x => x.Get<AppProfile>()).Returns(set.Object);

            var result = await _service.GetProfileByUserIdAsync(userId);

            Assert.Equal("p@mail.com", result.Email);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task GetProfileIdByUserIdIfExistsAsync_Should_Return_Id()
        {
            var userId = Guid.NewGuid();
            var profile = new AppProfile { UserId = userId, Id = Guid.NewGuid() };
            var set = new List<AppProfile> { profile }.BuildMockDbSet();

            _repoMock.Setup(x => x.Get<AppProfile>()).Returns(set.Object);

            var result = await _service.GetProfileIdByUserIdIfExistsAsync(userId);

            Assert.Equal(profile.Id, result);
        }

        [Fact]
        public async Task GetProfileIdByUserIdIfExistsAsync_Should_Return_Null_If_NotFound()
        {
            var set = new List<AppProfile>().BuildMockDbSet();
            _repoMock.Setup(x => x.Get<AppProfile>()).Returns(set.Object);

            var result = await _service.GetProfileIdByUserIdIfExistsAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateProfileAsync_Should_Modify_And_Save()
        {
            var id = Guid.NewGuid();
            var profile = new AppProfile { Id = id };
            var set = new List<AppProfile> { profile }.BuildMockDbSet();
            _repoMock.Setup(x => x.Get<AppProfile>()).Returns(set.Object);

            var model = new ProfileUpdateModel
            {
                Id = id,
                FirstName = "F",
                SurName = "S",
                LastName = "L",
                Email = "u@mail.com",
                Birthdate = new DateTimeOffset(2000, 2, 2, 0, 0, 0, TimeSpan.Zero),
                PhotoUrl = "url"
            };

            var result = await _service.UpdateProfileAsync(model);

            Assert.Equal("F", profile.FirstName);
            Assert.Equal("S", profile.SurName);
            Assert.Equal("L", profile.LastName);
            Assert.Equal("u@mail.com", profile.Email);
            Assert.Equal("url", profile.PhotoUrl);
            _repoMock.Verify(x => x.Attach(profile), Times.Once);
            _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);

            Assert.Equal(profile.Email, result.Email);
        }
    }

}
