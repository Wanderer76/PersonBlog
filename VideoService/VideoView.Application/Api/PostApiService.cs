using Blog.Domain.Services.Models;
using Blog.Service.Models.Blog;
using Blog.Service.Models.File;
using Shared.Utils;
using System.Web;
using VideoView.Application.Controllers;
using ViewReacting.Domain.Models;

namespace VideoView.Application.Api
{
    //TODO сделать обычный сервис, пробрасывать заголовки оригинального запроса
    public static class PostApiService
    {
        private const string PostManifest = "api/Post/manifest";
        private const string DetailPost = "api/Post/detail";
        private const string UserPostInfo = "ViewHistory/userReaction";
        private const string CommonBlog = "api/Blog/blogViewerInfoByPost";

        public static async Task<Result<PostFileMetadataModel>> GetFileMetadataAsync(this IHttpClientFactory httpContextFactory, Guid postId)
        {
            var result = await httpContextFactory.CreateClient("Profile").GetFromJsonAsync<PostFileMetadataModel>($"{PostManifest}/{postId}");
            if (result == null)
            {
                return Result<PostFileMetadataModel>.Failure(new("404", "Не удалось найти данные"));
            }
            return Result<PostFileMetadataModel>.Success(result!);
        }

        public static async Task<Result<PostDetailViewModel>> GetPostDetailViewAsync(this IHttpClientFactory httpContextFactory, Guid postId)
        {
            try
            {
                var result = await httpContextFactory.CreateClient("Profile").GetFromJsonAsync<PostDetailViewModel>($"{DetailPost}/{postId}");
                if (result == null)
                {
                    return Result<PostDetailViewModel>.Failure(new("404", "Не удалось найти данные"));
                }
                return Result<PostDetailViewModel>.Success(result!);
            }
            catch (Exception ex)
            {
                return Result<PostDetailViewModel>.Failure(new(ex.Message));
            }
        }

        public static async Task<Result<BlogUserInfoViewModel>> GetBlogModelAsync(this IHttpClientFactory httpContextFactory, Guid postId)
        {
            try
            {
                var result = await httpContextFactory.CreateClient("Profile").GetFromJsonAsync<BlogUserInfoViewModel>($"{CommonBlog}/{postId}");
                if (result == null)
                {
                    return Result<BlogUserInfoViewModel>.Failure(new("404", "Не удалось найти данные"));
                }
                return Result<BlogUserInfoViewModel>.Success(result!);
            }
            catch (Exception ex)
            {
                return Result<BlogUserInfoViewModel>.Failure(new(ex.Message));
            }
        }

        public static async Task<Result<ReactionHistoryViewItem>> GetUserViewInfoAsync(this IHttpClientFactory httpContextFactory, Guid postId, Guid? userId, string remoteIp, Guid? blogId)
        {
            if (userId.HasValue)
            {
                try
                {
                    var result = await httpContextFactory.CreateClient("Reacting").GetFromJsonAsync<ReactionHistoryViewItem>($"{UserPostInfo}/{postId}/{userId.Value}?blogId={blogId}");
                    if (result.PostId == Guid.Empty)
                        return Result<ReactionHistoryViewItem>.Success(new());
                    return Result<ReactionHistoryViewItem>.Success(result!);
                }
                catch (Exception ex)
                {
                    return Result<ReactionHistoryViewItem>.Failure(new(ex.Message));
                }
            }
            return Result<ReactionHistoryViewItem>.Success(new());
        }
    }
}
