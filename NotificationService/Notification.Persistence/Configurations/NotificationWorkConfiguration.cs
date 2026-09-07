using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Entities;

namespace Notification.Persistence.Configurations;

internal sealed class NotificationWorkConfiguration : IEntityTypeConfiguration<NotificationWork>
{
    public void Configure(EntityTypeBuilder<NotificationWork> builder)
    {
        builder.ToTable("NotificationWork");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DedupKey).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb");
        builder.Property(x => x.DestinationKey).HasMaxLength(1000);
        builder.Property(x => x.LastError).HasMaxLength(200);
        builder.HasIndex(x => new { x.Kind, x.DedupKey }).IsUnique();
        builder.HasIndex(x => new { x.Kind, x.Status, x.AvailableAt, x.LeaseUntil });
        builder.HasOne<UserNotification>().WithMany().HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
