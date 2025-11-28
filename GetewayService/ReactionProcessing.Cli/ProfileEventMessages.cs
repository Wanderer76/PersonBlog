using Infrastructure.Models;

namespace ReactionProcessing.Cli
{
    internal class ProfileEventMessages
    {
        public object Id { get; set; }
        public string EventData { get; set; }
        public string EventType { get; set; }
        public EventState State { get; set; }
    }
}