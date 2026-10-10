using System.Collections.Generic;

namespace ApartmentResidenceManagement.Domain.Entities;

public class Resident
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    // Navigation properties
    public virtual ICollection<ResidenceHistory> ResidenceHistories { get; set; } = new List<ResidenceHistory>();
}

