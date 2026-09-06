using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Entities;

namespace Notification.Persistence.Configurations;

internal sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).ValueGeneratedNever();
        builder.Property(notification => notification.Payload).IsRequired();
        builder.HasIndex(notification => new { notification.UserId, notification.CreatedAt, notification.Id });
    }
}

internal sealed class UserNotificationTypesConfiguration : IEntityTypeConfiguration<UserNotificationTypes>
{
    public void Configure(EntityTypeBuilder<UserNotificationTypes> builder)
    {
        builder.ToTable("UserNotificationTypes");
        builder.HasKey(link => new { link.UserNotificationId, link.NotificationTypeId });
        builder.HasOne(link => link.UserNotification)
            .WithMany(notification => notification.NotificationTypes)
            .HasForeignKey(link => link.UserNotificationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.NotificationType)
            .WithMany()
            .HasForeignKey(link => link.NotificationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class NotificationTypeConfiguration : IEntityTypeConfiguration<NotificationType>
{
    public void Configure(EntityTypeBuilder<NotificationType> builder)
    {
        builder.ToTable("NotificationTypes");
        builder.HasKey(type => type.Id);
        builder.Property(type => type.Name).IsRequired();
    }
}
