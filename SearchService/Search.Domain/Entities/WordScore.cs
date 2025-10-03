
namespace Search.Domain.Entities
{
    public class WordScore : ISearch
    {
        public long Id { get; private set; }
        public string Word { get; set; }
        public double Score { get; set; }

        private WordScore()
        {
            
        }
        public WordScore(string word, double score)
        {
            Word = word;
            Score = score;
        }
    }
}
