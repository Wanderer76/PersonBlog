using PlayListService.Domain.Entities;

namespace PlayListServiceTests;

public sealed class PlayListDomainTests
{
    [Fact]
    public void Create_RejectsDuplicatePosts()
    {
        var postId = Guid.NewGuid();

        var result = PlayList.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "My playlist",
            Guid.NewGuid(),
            thumbnailId: null,
            PlayListContentType.Video,
            PlayListKind.Authored,
            [postId, postId]);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    public void ChangeVideoPosition_RejectsDestinationOutsidePlaylist(int destination)
    {
        var playlist = CreatePlaylistWithThreePosts();
        var postId = playlist.PlayListItems[1].PostId;

        var result = playlist.ChangeVideoPosition(postId, destination);

        Assert.True(result.IsFailure);
        Assert.Equal([1, 2, 3], playlist.PlayListItems.OrderBy(x => x.Position).Select(x => x.Position));
    }

    [Fact]
    public void ChangeVideoPosition_MovesPostAndKeepsPositionsContiguous()
    {
        var playlist = CreatePlaylistWithThreePosts();
        var firstPostId = playlist.PlayListItems[0].PostId;
        var secondPostId = playlist.PlayListItems[1].PostId;
        var thirdPostId = playlist.PlayListItems[2].PostId;

        var result = playlist.ChangeVideoPosition(firstPostId, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [secondPostId, thirdPostId, firstPostId],
            playlist.PlayListItems.OrderBy(x => x.Position).Select(x => x.PostId));
        Assert.Equal([1, 2, 3], playlist.PlayListItems.OrderBy(x => x.Position).Select(x => x.Position));
    }

    [Fact]
    public void RemoveVideo_ReturnsFailureAndKeepsPlaylist_WhenPostDoesNotExist()
    {
        var playlist = CreatePlaylistWithThreePosts();
        var originalPosts = playlist.PlayListItems.OrderBy(x => x.Position).Select(x => x.PostId).ToArray();

        var result = playlist.RemoveVideo(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(originalPosts, playlist.PlayListItems.OrderBy(x => x.Position).Select(x => x.PostId));
    }

    [Fact]
    public void RemoveVideo_CompactsRemainingPositions()
    {
        var playlist = CreatePlaylistWithThreePosts();

        var result = playlist.RemoveVideo(playlist.PlayListItems[1].PostId);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2], playlist.PlayListItems.OrderBy(x => x.Position).Select(x => x.Position));
    }

    [Fact]
    public void AddPost_RejectsPostWithDifferentContentType()
    {
        var playlist = CreatePlaylistWithThreePosts();

        var result = playlist.AddPost(Guid.NewGuid(), PlayListContentType.Text, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(3, playlist.PlayListItems.Count);
    }

    private static PlayList CreatePlaylistWithThreePosts() =>
        PlayList.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "My playlist",
            Guid.NewGuid(),
            thumbnailId: null,
            PlayListContentType.Video,
            PlayListKind.Authored,
            [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]).Value;
}
