using ApartmentResidenceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApartmentResidenceManagement.Infrastructure.Configurations;

public class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("NotificationRecipients");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.IsRead)
            .IsRequired();

        builder.Property(r => r.ReadAt);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne(r => r.Notification)
            .WithMany(n => n.Recipients)
            .HasForeignKey(r => r.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.UserAccount)
            .WithMany()
            .HasForeignKey(r => r.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(r => new { r.UserAccountId, r.IsRead });
    }
}
