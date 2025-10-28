
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models.Entities;
public class Vital
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g., BP, Temp
    public string Value { get; set; } = string.Empty;
    public Guid PatientRecordId { get; set; }
}
