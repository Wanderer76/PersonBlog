namespace Recommendation.Services.Options;

public sealed class AffinityOptions
{
    public const string SectionName = "Recommendation:Affinity";

    public double ImpressionWeight { get; set; }
    public double OpenWeight { get; set; } = 0.5;
    public double ViewProgressWeight { get; set; } = 0.5;
    public double LongViewWeight { get; set; } = 3;
    public double CompletedViewWeight { get; set; } = 4;
    public double LikeWeight { get; set; } = 5;
    public double DislikeWeight { get; set; } = -6;
    public double LongViewThreshold { get; set; } = 0.7;
    public double HalfLifeDays { get; set; } = 30;
}
