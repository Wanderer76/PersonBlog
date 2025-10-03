using Search.Domain.Entities;

namespace Search.Domain.Models
{
    public class TokenizerRequest
    {
        public string Text { get; set; }
    }

    public class TokenizeWord
    {
        public string Word { get; set; }
        public double Score {  get; set; }
    }

    public class TokenizerResponse
    {
        public List<TokenizeWord> Tokens { get; set; }
    }
}
