using Blog.Service.Models;
using Blog.Service.Models.Blog;
using Blog.Service.Models.File;
using Shared.Utils;
using System.Web;

namespace VideoView.Application.Api
{
    public static class PostApiService
    {
        private const string PostManifest = "api/Post/manifest";
        private const string DetailPost = "api/Post/detail";
        private const string UserPostInfo = "api/Post/userInfo";
        private const string CommonBlog = "api/Blog/blogByPost";

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

        public static async Task<Result<BlogModel>> GetBlogModelAsync(this IHttpClientFactory httpContextFactory, Guid postId)
        {
           var result = await httpContextFactory.CreateClient("Profile").GetFromJsonAsync<BlogModel>($"{CommonBlog}/{postId}");
            if (result == null)
            {
                return Result<BlogModel>.Failure(new("404", "Не удалось найти данные"));
            }
            return Result<BlogModel>.Success(result!);
        }

        public static async Task<Result<UserViewInfo>> GetUserViewInfoAsync(this IHttpClientFactory httpContextFactory, Guid postId, Guid? userId, string remoteIp)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (userId.HasValue)
            {
                query["userId"] = HttpUtility.UrlEncode(userId.Value.ToString());
            }
            query["address"] = HttpUtility.UrlEncode(remoteIp);


            var result = await httpContextFactory.CreateClient("Profile").GetFromJsonAsync<UserViewInfo>($"{UserPostInfo}/{postId}?{query}");
            if (result == null)
            {
                return Result<UserViewInfo>.Failure(new("404", "Не удалось найти данные"));
            }
            return Result<UserViewInfo>.Success(result!);
        }
    }
}
