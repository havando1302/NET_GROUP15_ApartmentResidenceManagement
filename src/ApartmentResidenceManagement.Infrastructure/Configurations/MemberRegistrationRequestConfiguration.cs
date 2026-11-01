using ApartmentResidenceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApartmentResidenceManagement.Infrastructure.Configurations;

public class MemberRegistrationRequestConfiguration : IEntityTypeConfiguration<MemberRegistrationRequest>
{
    public void Configure(EntityTypeBuilder<MemberRegistrationRequest> builder)
    {
        builder.ToTable("MemberRegistrationRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.IdentityCard)
            .HasMaxLength(20);

        builder.Property(r => r.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(r => r.Email)
            .HasMaxLength(100);

        builder.Property(r => r.RejectReason)
            .HasMaxLength(500);

        builder.Property(r => r.RelationshipType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne(r => r.Apartment)
            .WithMany()
            .HasForeignKey(r => r.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RequesterResident)
            .WithMany()
            .HasForeignKey(r => r.RequesterResidentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
