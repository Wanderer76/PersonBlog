using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Domain.Events;
using Profile.Domain.Models;
using Profile.Domain.Services;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Profile.Service.Implementation
{
    internal class DefaultPostBanService : IBanService
    {
        private readonly IReadWriteRepository<IUserEntity> _readWriteRepository;
        private readonly ICurrentUserService _currentUserService;

        public DefaultPostBanService(IReadWriteRepository<IUserEntity> readWriteRepository, ICurrentUserService currentUserService)
        {
            _readWriteRepository = readWriteRepository;
            _currentUserService = currentUserService;
        }

        public async Task<Result> SendPostBanRequest(PostReport report)
        {
            var user = await _currentUserService.GetCurrentUserAsync();

            if (user.IsAnonymous)
            {
                return Result.Failure(new Error("Forbbiden"));
            }
            if(user.UserId != report.UserId)
            {
                return Result.Failure(new Error("Forbbiden"));
            }
            var profile = await _readWriteRepository.Get<AppProfile>()
                .Where(x => x.UserId == user.UserId)
                .FirstAsync();

            var message = new PostBanRequest(profile.Id, report.Message, report.ReasonId, report.PostId);
            _readWriteRepository.Add(message);
            var @event = new PostBanEvent
            {
                PostId = report.PostId,
                ObjectName = report.ObjectName,
                ReasonId = report.ReasonId,
                UserMessage = report.Message,
                CreatorUserId = user.UserId,
                CreatedAt = DateTimeService.Now(),
            };
            _readWriteRepository.Add(ReactingEvent.Create(@event, GuidService.GetNewGuid()));
            await _readWriteRepository.SaveChangesAsync();
            return Result.Success();
        }
    }
}
