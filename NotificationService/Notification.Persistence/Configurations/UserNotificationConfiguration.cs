using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Entities;

namespace Notification.Persistence.Configurations;

internal sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Content).HasConversion(
            content => Serialize(content), json => Deserialize(json)).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Producer).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Kind, x.BusinessId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt, x.Id }).IsDescending(false, true, true);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt, x.Id }, "IX_Notifications_Unread")
            .HasFilter("\"ReadAt\" IS NULL");
        builder.Ignore(x => x.Source);
        builder.Ignore(x => x.IsRead);
    }

    private static string Serialize(NotificationContent content) => JsonSerializer.Serialize(content, Json);

    private static NotificationContent Deserialize(string json)
    {
        var content = JsonSerializer.Deserialize<NotificationContent>(json, Json)!;
        return content with
        {
            Data = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(content.Data))
        };
    }
}
