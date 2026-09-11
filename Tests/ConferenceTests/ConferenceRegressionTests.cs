using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Conference.Domain.Entities;
using Conference.Domain.Models;
using Conference.Persistence;
using Conference.Service.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConferenceTests;

public sealed class ConferenceRegressionTests
{
    [Fact]
    public void ConferenceHub_RequiresAuthorization()
    {
        var authorize = typeof(ConferenceHub).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
    }

    [Fact]
    public void ConferenceModel_PersistsPostIdAndParticipantUniqueness()
    {
        var options = new DbContextOptionsBuilder<ConferenceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ConferenceDbContext(options);

        var room = context.Model.FindEntityType(typeof(ConferenceRoom));
        var participant = context.Model.FindEntityType(typeof(ConferenceParticipant));
        var invitation = context.Model.FindEntityType(typeof(ConferenceInvitation));
        var outbox = context.Model.FindEntityType(typeof(ConferenceOutboxMessage));

        Assert.Equal("Conference", context.Model.GetDefaultSchema());
        Assert.NotNull(room?.FindProperty(nameof(ConferenceRoom.PostId)));
        Assert.Contains(participant!.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ConferenceParticipant.ConferenceRoomId), nameof(ConferenceParticipant.UserId)]));
        Assert.Contains(invitation!.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ConferenceInvitation.ConferenceId), nameof(ConferenceInvitation.RecipientUserId)]));
        Assert.NotNull(outbox);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MessageForm_RejectsEmptyText(string? text)
    {
        var form = new CreateMessageForm { Message = text! };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            form,
            new ValidationContext(form),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
    }
}
