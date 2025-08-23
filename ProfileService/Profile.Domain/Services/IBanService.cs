using Profile.Domain.Models;
using Shared.Utils;

namespace Profile.Domain.Services;

public interface IBanService
{
    Task<Result> SendPostBanRequest(PostReport report);
}
