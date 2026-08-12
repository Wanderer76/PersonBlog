using Microsoft.Extensions.Options;
using Recommendation.Contracts.Events;
using Recommendation.Services.Options;

namespace Recommendation.Services.Services;

public sealed class AffinityCalculator
{
    private readonly AffinityOptions _options;

    public AffinityCalculator(IOptions<AffinityOptions> options)
    {
        _options = options.Value;
        if (_options.HalfLifeDays <= 0) throw new ArgumentOutOfRangeException(nameof(options), "Half-life must be positive.");
        if (_options.LongViewThreshold is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(options), "Long-view threshold must be between 0 and 1.");
    }

    public double GetDelta(UserInteractionRecordedV1 interaction) => interaction.Type switch
    {
        UserInteractionType.Impression => _options.ImpressionWeight,
        UserInteractionType.Open => _options.OpenWeight,
        UserInteractionType.ViewProgress when interaction.WatchRatio >= _options.LongViewThreshold => _options.LongViewWeight,
        UserInteractionType.ViewProgress => _options.ViewProgressWeight,
        UserInteractionType.ViewCompleted => _options.CompletedViewWeight,
        UserInteractionType.Like => _options.LikeWeight,
        UserInteractionType.Dislike => _options.DislikeWeight,
        UserInteractionType.ReactionRemoved when interaction.PreviousReaction == true => -_options.LikeWeight,
        UserInteractionType.ReactionRemoved when interaction.PreviousReaction == false => -_options.DislikeWeight,
        UserInteractionType.ReactionRemoved => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(interaction.Type))
    };

    public (double Score, DateTimeOffset UpdatedAt) Apply(
        double currentScore,
        DateTimeOffset currentUpdatedAt,
        double delta,
        DateTimeOffset occurredAt)
    {
        if (occurredAt >= currentUpdatedAt)
        {
            return (
                Decay(currentScore, occurredAt - currentUpdatedAt) + delta,
                occurredAt);
        }

        return (
            currentScore + Decay(delta, currentUpdatedAt - occurredAt),
            currentUpdatedAt);
    }

    private double Decay(double score, TimeSpan age) =>
        score * Math.Pow(0.5, age.TotalDays / _options.HalfLifeDays);
}
