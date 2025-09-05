using Shared.Services;
using Shared.Utils;

namespace MusicRecommendation.Domain.Domain
{
    public class UserTrackAudition : IRecommendation
    {
        public Guid UserId { get; private set; }
        public Guid TrackId { get; private set; }
        public int Count { get; private set; }
        public DateTimeOffset UpdateDate { get; private set; }

        private UserTrackAudition()
        {

        }

        public UserTrackAudition(Guid userId, Guid trackId, int count)
        {
            UserId = userId;
            TrackId = trackId;
            Count = count;
            UpdateDate = DateTimeService.Now().Date.ToUniversalTime();
        }

        public Result UpdateCount(int count)
        {
            if (count < Count)
            {
                return Result.Failure(new Error("count couldn't be less than current"));
            }
            Count = count;
            UpdateDate = DateTimeService.Now().Date.ToUniversalTime();
            return Result.Success();
        }
    }
}
