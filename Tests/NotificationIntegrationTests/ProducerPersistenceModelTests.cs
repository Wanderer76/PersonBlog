using Comments.Domain.Entities;
using Comments.Persistence;
using Conference.Domain.Entities;
using Conference.Persistence;
using Microsoft.EntityFrameworkCore;

namespace NotificationIntegrationTests;

public sealed class ProducerPersistenceModelTests
{
    [Fact]
    public void CommentsModelContainsOutboxAndMigration()
    {
        var options = new DbContextOptionsBuilder<CommentDbContext>()
            .UseNpgsql("Host=localhost;Database=model_check;Username=test;Password=test")
            .Options;
        using var context = new CommentDbContext(options);

        Assert.NotNull(context.Model.FindEntityType(typeof(CommentOutboxMessage)));
        Assert.Contains("20260910120000_AddCommentReplyOutbox", context.Database.GetMigrations());
    }

    [Fact]
    public void ConferenceModelContainsInvitationOutboxAndMigration()
    {
        var options = new DbContextOptionsBuilder<ConferenceDbContext>()
            .UseNpgsql("Host=localhost;Database=model_check;Username=test;Password=test")
            .Options;
        using var context = new ConferenceDbContext(options);

        Assert.NotNull(context.Model.FindEntityType(typeof(ConferenceInvitation)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ConferenceOutboxMessage)));
        Assert.Contains("20260910120500_AddConferenceInvitationsAndOutbox", context.Database.GetMigrations());
    }
}
