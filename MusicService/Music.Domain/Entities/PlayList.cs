using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System.ComponentModel.DataAnnotations.Schema;

namespace Music.Domain.Entities
{
    public class PlayListConstants
    {
        public const string UploadPlaylistName = "Загруженные";
        public const string LikedTracksPlayList = "Мне нравится";

    }


    public class PlayList : IMusicEntity
    {
        public Guid Id { get; private set; }
        public string Name { get; set; }
        public Guid UserId { get; private set; }
        public ConstPlayListType Type { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public List<PlayListTrack> Tracks { get; private set; } = [];

        public PlayList()
        {

        }

        public PlayList(string name, Guid userId, ConstPlayListType type, List<PlayListTrack> tracks)
        {
            Id = GuidService.GetNewGuid();
            Name = name;
            UserId = userId;
            Type = type;
            CreatedAt = DateTimeService.Now();
            Tracks = tracks;
        }

        public Result AddTrack(Track track)
        {
            if (Tracks.Any(x => x.TrackId == track.Id))
            {
                return Result.Failure(new Error("Duplicate element"));
            }
            Tracks.Add(new PlayListTrack(Id, track.Id));
            return Result.Success();
        }

        public async Task<Result> AddTrackAsync(IReadRepository<IMusicEntity> read, Guid trackId)
        {
            if (await read.Get<PlayListTrack>().AnyAsync(x => x.TrackId == trackId && x.PlayListId == Id))
            {
                return Result.Failure(new Error("Duplicate element"));
            }
            Tracks.Add(new PlayListTrack(Id, trackId));
            return Result.Success();
        }
    }

    public class PlayListTrack : IMusicEntity
    {
        public Guid PlayListId { get; private set; }
        public Guid TrackId { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        [ForeignKey(nameof(PlayListId))]
        public PlayList PlayList { get; private set; }
        [ForeignKey(nameof(TrackId))]
        public Track Track { get; private set; }
        public PlayListTrack()
        {

        }

        public PlayListTrack(Guid playListId, Guid trackId)
        {
            PlayListId = playListId;
            TrackId = trackId;
            CreatedAt = DateTimeService.Now();
        }
    }

    public enum ConstPlayListType
    {
        Upload,
        Liked,
        FavouritePlayLists,
        Created,
    }
}
