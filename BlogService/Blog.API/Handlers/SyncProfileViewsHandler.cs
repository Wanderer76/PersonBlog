﻿using Blog.Contracts.Events;
using Blog.Domain.Entities;
using Blog.Domain.Services.Models;
using Infrastructure.Services;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using System.Net;
using System.Text.Json;

namespace Blog.API.Handlers
{
    public class SyncProfileViewsHandler : IEventHandler<UserViewedSyncEvent>, IEventHandler<UserReactionSyncEvent>
    {
        private readonly IReadWriteRepository<IBlogEntity> _context;
        private readonly ICacheService _cacheService;

        public SyncProfileViewsHandler(IReadWriteRepository<IBlogEntity> context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task Handle(IMessageContext<UserViewedSyncEvent> @event)
        {
            var userId = @event.Message.UserId;
            var ipAddress = @event.Message.RemoteIp;

            var post = await _context.Get<Post>()
                .FirstAsync(x => x.Id == @event.Message.PostId);

            var existView = await _context.Get<PostViewer>()
                .Where(x => x.PostId == post.Id)
                .Where(x => x.UserId == userId || x.UserIpAddress == ipAddress)
                .FirstOrDefaultAsync();

            _context.Attach(post);

            if (existView == null)
            {
                if (@event.Message.IsViewed)
                {
                    post.ViewCount++;
                }
                existView = new PostViewer
                {
                    Id = GuidService.GetNewGuid(),
                    PostId = @event.Message.PostId,
                    UserId = userId,
                    UserIpAddress = ipAddress ?? string.Empty,
                    IsViewed = @event.Message.IsViewed,
                };
                _context.Add(existView);
            }
            else
            {
                if (@event.Message.IsViewed)
                {
                    post.ViewCount++;
                }
                _context.Attach(existView);
                existView.UserId = userId;
                existView.UserIpAddress = ipAddress ?? string.Empty;
                existView.IsViewed = @event.Message.IsViewed;
            }
            await _cacheService.RemoveCachedDataAsync(new PostDetailViewModelCacheKey(post.Id));
            var postUpdateEvent = new PostUpdateEvent
            {
                BlogId = post.BlogId,
                PostId = post.Id,
                CreatedAt = post.CreatedAt,
                Description = post.Description,
                Title = post.Title,
                UpdateType = UpdateType.Update,
                ViewCount = post.ViewCount,
            };
            _context.Add(new VideoProcessEvent { EventData = JsonSerializer.Serialize(postUpdateEvent), EventType = nameof(PostUpdateEvent) });
            await _context.SaveChangesAsync();
        }

        public async Task Handle(IMessageContext<UserReactionSyncEvent> @event)
        {
            var existView = await _context.Get<PostViewer>()
            .Where(x => x.PostId == @event.Message.PostId)
            .Where(x => x.UserId == @event.Message.UserId || x.UserIpAddress == @event.Message.RemoteIp)
            .FirstOrDefaultAsync();

            var post = await _context.Get<Post>()
                .AsTracking()
                .FirstAsync(x => x.Id == @event.Message.PostId);
            if (existView == null)
            {
                if (@event.Message.IsLike == true)
                {
                    post.LikeCount++;
                }
                if (@event.Message.IsLike == false)
                {
                    post.DislikeCount++;
                }
                existView = new PostViewer
                {
                    Id = GuidService.GetNewGuid(),
                    PostId = @event.Message.PostId,
                    IsLike = @event.Message.IsLike,
                    UserId = @event.Message.UserId,
                    UserIpAddress = @event.Message.RemoteIp ?? string.Empty
                };
                _context.Add(existView);
            }
            else
            {
                if (@event.Message.IsLike.HasValue)
                {
                    if (@event.Message.IsLike == true)
                    {
                        post.LikeCount = Math.Max(post.LikeCount + (existView?.IsLike == true ? -1 : 1), 0);
                        post.DislikeCount = Math.Max(post.DislikeCount + (existView?.IsLike == false ? -1 : 0), 0);
                    }
                    else
                    {
                        post.LikeCount = Math.Max(post.LikeCount + (existView?.IsLike == true ? -1 : 0), 0);
                        post.DislikeCount = Math.Max(post.DislikeCount + (existView?.IsLike == false ? -1 : 1), 0);
                    }
                }
                else
                {
                    if (existView?.IsLike == true)
                    {
                        post.LikeCount = Math.Max(post.LikeCount - 1, 0);
                    }
                    else if (existView?.IsLike == false)
                    {
                        post.DislikeCount = Math.Max(post.DislikeCount - 1, 0);
                    }
                }

                _context.Attach(existView);
                existView.IsLike = @event.Message.IsLike == existView.IsLike ? null : @event.Message.IsLike;
            }
            await _context.SaveChangesAsync();
            await _cacheService.RemoveCachedDataAsync(new PostDetailViewModelCacheKey(post.Id));
        }

        //public async Task Handle(MessageContext @event)
        //{
        //    switch (@event.Message)
        //    {
        //        case UserReactionSyncEvent command:
        //            await Handle(MessageContext.Create(@event.CorrelationId, command));
        //            break;
        //        case UserViewedSyncEvent response:
        //            await Handle(MessageContext.Create(@event.CorrelationId, response));
        //            break;
        //        default:
        //            throw new ArgumentException();
        //    }
        //}
    }
}
