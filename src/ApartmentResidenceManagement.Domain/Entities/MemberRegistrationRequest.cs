using System;
using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Domain.Entities;

public class MemberRegistrationRequest
{
    public int Id { get; set; }
    public int ApartmentId { get; set; }
    public int RequesterResidentId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? IdentityCard { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public RelationshipType RelationshipType { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ProcessedAt { get; set; }
    public string? RejectReason { get; set; }

    // Navigation properties
    public virtual Apartment Apartment { get; set; } = null!;
    public virtual Resident RequesterResident { get; set; } = null!;
}
