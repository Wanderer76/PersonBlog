using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Music.Contract.Models.PlayList;
using Music.Domain.Entities;
using Music.Domain.Services;
using Shared.Persistence;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music.Service.Services
{
    internal class DefaultPlayListService : IMusicPlayListService
    {
        private readonly IReadWriteRepository<IMusicEntity> _repository;
        private readonly ICurrentUserService _currentUserService;

        public DefaultPlayListService(IReadWriteRepository<IMusicEntity> repository, ICurrentUserService currentUserService)
        {
            _repository = repository;
            _currentUserService = currentUserService;
        }

        public async Task<Result<IReadOnlyList<PlayListViewModel>>> CreateDefaultUserPlayListsAsync()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            return await CreateDefaultUserPlayListsAsync(currentUser.UserId.Value);
        }

        public async Task<Result<IReadOnlyList<PlayListViewModel>>> CreateDefaultUserPlayListsAsync(Guid userId)
        {
            var defaultPlayList = await _repository.Get<PlayList>()
                .Where(x => x.UserId == userId)
                .Where(x => x.Type == ConstPlayListType.Liked || x.Type == ConstPlayListType.Upload)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    TracksCount = x.Tracks.Count,
                    x.Type
                }
                )
                .ToListAsync();

            var result = defaultPlayList
                    .Select(x => new PlayListViewModel(x.Id, x.Name, x.TracksCount, null, x.Type.ToString()))
                    .ToList();

            if (result.Count == 2)
            {
                return result;
            }

            if (!defaultPlayList.Any(x => x.Type == ConstPlayListType.Liked))
            {
                var liked = new PlayList(PlayListConstants.LikedTracksPlayList, userId, ConstPlayListType.Liked, []);
                _repository.Add(liked);
                result.Add(new PlayListViewModel(liked.Id, liked.Name, 0, null, liked.Type.ToString()));
            }
            if (!defaultPlayList.Any(x => x.Type == ConstPlayListType.Upload))
            {

                var upload = new PlayList(PlayListConstants.UploadPlaylistName, userId, ConstPlayListType.Upload, []);
                _repository.Add(upload);

                result.Add(new PlayListViewModel(upload.Id, upload.Name, 0, null, upload.Type.ToString()));
            }
            await _repository.SaveChangesAsync();

            return result;
        }

        public Task<Result<PlayListViewModel>> CreatePlayListsAsync(CreatePlayListRequest createPlayList)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<IReadOnlyList<PlayListViewModel>>> GetCurrentUserPlayListsAsync()
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var result = await _repository.Get<PlayList>()
                         .Where(x => x.UserId == user.UserId.Value)
                         .Where(x => x.Type == ConstPlayListType.Liked || x.Type == ConstPlayListType.Upload)
                         .Select(x => new
                         {
                             x.Id,
                             x.Name,
                             TracksCount = x.Tracks.Count,
                             x.Type
                         })
                         .AsAsyncEnumerable()
                         .Select(x => new PlayListViewModel(x.Id, x.Name, x.TracksCount, null, x.Type.ToString()))
                         .ToListAsync();
            if (result.Count > 0)
                return result;
            if(result.Count == 0)
            {
                var playLists = await CreateDefaultUserPlayListsAsync(user.UserId.Value);
                    return playLists;
            }
            return Result<IReadOnlyList<PlayListViewModel>>.Failure(new Error("Somethig went wrong"));
        }
    }
}
