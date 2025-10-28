
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models.Entities;
public class Prescription
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Medication { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public Guid PrescribedByUserId { get; set; } // doctor Id
    public Guid PatientRecordId { get; set; }
}