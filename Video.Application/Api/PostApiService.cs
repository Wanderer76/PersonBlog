using Blog.Service.Models;
using Blog.Service.Models.Blog;
using Blog.Service.Models.File;
using Shared.Utils;
using System.Web;
using ViewReacting.Domain.Models;
using static MassTransit.ValidationResultExtensions;

namespace VideoView.Application.Api
{
    public static class PostApiService
    {
        private const string PostManifest = "api/Post/manifest";
        private const string DetailPost = "api/Post/detail";
        private const string UserPostInfo = "ViewHistory/item";
        private const string CommonBlog = "api/Blog/blogViewerInfoByPost";

        public static async Task<Result<FileMetadataModel>> GetFileMetadataAsync(this IHttpClientFactory httpContextFactory, Guid postId)
        {
            var result = await httpContextFactory.CreateClient("Profile").GetFromJsonAsync<FileMetadataModel>($"{PostManifest}/{postId}");
            if (result == null)
            {
                return Result<FileMetadataModel>.Failure(new("404", "Не удалось найти данные"));
            }
            return Result<FileMetadataModel>.Success(result!);
        }

        public static async Task<Result<PostDetailViewModel>> GetPostDetailViewAsync(this IHttpClientFactory httpContextFactory, Guid postId)
        {
            var result = await httpContextFactory.CreateClient("Profile").GetFromJsonAsync<PostDetailViewModel>($"{DetailPost}/{postId}");
            if (result == null)
            {
                return Result<PostDetailViewModel>.Failure(new("404", "Не удалось найти данные"));
            }
            return Result<PostDetailViewModel>.Success(result!);
        }

        public static async Task<Result<BlogUserInfoViewModel>> GetBlogModelAsync(this IHttpClientFactory httpContextFactory, Guid postId)
        {
            var result = await httpContextFactory.CreateClient("Profile").GetFromJsonAsync<BlogUserInfoViewModel>($"{CommonBlog}/{postId}");
            if (result == null)
            {
                return Result<BlogUserInfoViewModel>.Failure(new("404", "Не удалось найти данные"));
            }
            return Result<BlogUserInfoViewModel>.Success(result!);
        }

        public static async Task<Result<HistoryViewItem>> GetUserViewInfoAsync(this IHttpClientFactory httpContextFactory, Guid postId, Guid? userId, string remoteIp)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (userId.HasValue)
            {
                query["userId"] = HttpUtility.UrlEncode(userId.Value.ToString());
            }
            query["address"] = HttpUtility.UrlEncode(remoteIp);

            if (userId.HasValue)
            {
                var result = await httpContextFactory.CreateClient("Reacting").GetFromJsonAsync<HistoryViewItem>($"{UserPostInfo}/{postId}/{userId.Value}");
                if (result.PostId == Guid.Empty)
                    return Result<HistoryViewItem>.Success(null);
                return Result<HistoryViewItem>.Success(result!);
            }
            return Result<HistoryViewItem>.Success(null);
        }
    }
}
