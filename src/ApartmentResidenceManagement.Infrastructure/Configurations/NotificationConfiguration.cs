using ApartmentResidenceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApartmentResidenceManagement.Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(n => n.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(n => n.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(n => n.TargetAudience)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(100);

        builder.Property(n => n.RelatedEntityId);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.HasOne(n => n.Sender)
            .WithMany()
            .HasForeignKey(n => n.SenderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(n => n.Recipients)
            .WithOne(r => r.Notification)
            .HasForeignKey(r => r.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
