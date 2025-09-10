using Search.Domain.Entities;

namespace Search.Domain.Models
{
    public class TokenizerRequest
    {
        public string Text { get; set; }
    }

    public class TokenizerResponse
    {
        public List<WordScore> Tokens { get; set; }
    }
}
