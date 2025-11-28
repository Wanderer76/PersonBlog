using Infrastructure.Models;
using Video.Domain.Entities;

namespace Video.Domain.Events
{
    public class VideoEvent : BaseEvent, IVideoViewEntity
    {
    }
}
