﻿using Blog.Domain.Entities;
using Shared.Services;

namespace Blog.Domain.Services.Models
{
    public class PostDetailViewModel
    {
        public Guid Id { get; }
        public string? PreviewUrl { get; }
        public DateTimeOffset CreatedAt { get; }
        public int ViewCount { get; set; }
        public string? Description { get; }
        public string Title { get; }
        public PostType Type { get; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public VideoMetadataModel? VideoData { get; }
        public bool IsProcessed { get; }

        public PostDetailViewModel(Guid id, string? previewUrl, DateTimeOffset createdAt, int viewCount, string? description, string title, PostType type, int likeCount, int dislikeCount, VideoMetadataModel? videoData, bool isProcessed)
        {
            Id = id;
            PreviewUrl = previewUrl;
            CreatedAt = createdAt;
            ViewCount = viewCount;
            Description = description;
            Title = title;
            Type = type;
            LikeCount = likeCount;
            DislikeCount = dislikeCount;
            VideoData = videoData;
            IsProcessed = isProcessed;
        }

        public override bool Equals(object? obj)
        {
            return obj is PostDetailViewModel other &&
                   Id.Equals(other.Id) &&
                   PreviewUrl == other.PreviewUrl &&
                   CreatedAt.Equals(other.CreatedAt) &&
                   ViewCount == other.ViewCount &&
                   Description == other.Description &&
                   Title == other.Title &&
                   Type == other.Type &&
                   EqualityComparer<VideoMetadataModel?>.Default.Equals(VideoData, other.VideoData) &&
                   IsProcessed == other.IsProcessed;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Id);
            hash.Add(PreviewUrl);
            hash.Add(CreatedAt);
            hash.Add(ViewCount);
            hash.Add(Description);
            hash.Add(Title);
            hash.Add(Type);
            hash.Add(VideoData);
            hash.Add(IsProcessed);
            return hash.ToHashCode();
        }
    }

    public readonly struct PostDetailViewModelCacheKey : ICacheKey
    {
        private readonly Guid Id;
        private const string Key = nameof(PostDetailViewModelCacheKey);

        public PostDetailViewModelCacheKey(Guid id)
        {
            Id = id;
        }

        public string GetKey() => $"{Key}:{Id}";
    }
}
