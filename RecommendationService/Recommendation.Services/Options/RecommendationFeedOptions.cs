namespace Recommendation.Services.Options;

public sealed class RecommendationFeedOptions
{
    public const string SectionName = "Recommendation:Feed";

    public string AlgorithmVersion { get; set; } = "heuristic-v1";
    public string CursorSigningKey { get; set; } = string.Empty;
    public int DefaultLimit { get; set; } = 20;
    public int MaxLimit { get; set; } = 100;
    public int CandidatePoolSize { get; set; } = 500;
    public int MaxPostsPerBlog { get; set; } = 2;
    public int SeenWindowDays { get; set; } = 30;
    public double SeenPenalty { get; set; } = 0.4;
}
