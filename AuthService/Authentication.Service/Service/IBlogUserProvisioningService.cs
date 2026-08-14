using Shared.Models;

namespace Authentication.Service.Service;

public interface IBlogUserProvisioningService
{
    Task<UserModel> ProvisionBlogAsync(Guid userId, Guid blogId);
}
