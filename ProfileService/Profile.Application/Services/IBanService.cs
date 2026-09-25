using Profile.Application.Models;

namespace Profile.Application.Services;

public interface IBanService
{
    Task<Result> SendPostBanRequest(PostReport report);
}
