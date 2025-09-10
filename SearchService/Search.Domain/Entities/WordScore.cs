
namespace Search.Domain.Entities
{
    public class WordScore
    {
        public long Id { get; private set; }
        public string Word { get; set; }
        public double Score { get; set; }
    }
}
