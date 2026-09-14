namespace Profile.Application.Models;

public class HasSubscriptionModel
{
    public Guid BlogId { get; }
    public bool HasSubscription { get; }

    public HasSubscriptionModel(Guid blogId, bool hasSubscription)
    {
        BlogId = blogId;
        HasSubscription = hasSubscription;
    }
}
