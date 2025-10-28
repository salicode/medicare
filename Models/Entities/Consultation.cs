using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models.Entities;

public class Consultation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PatientRecordId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    public Guid? NurseId { get; set; } // Assigned nurse

    [Required]
    public DateTime ScheduledAt { get; set; }

    [Required]
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);

    [Required]
    public ConsultationType ConsultationType { get; set; }

    [Required]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    [MaxLength(1000)]
    public string? Symptoms { get; set; }

    [MaxLength(1000)]
    public string? Diagnosis { get; set; }

    [MaxLength(1000)]
    public string? TreatmentPlan { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public decimal Fee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    [ForeignKey("PatientRecordId")]
    public PatientRecord PatientRecord { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public Doctor Doctor { get; set; } = null!;

    [ForeignKey("NurseId")]
    public User? Nurse { get; set; }

    public ICollection<ConsultationDocument> Documents { get; set; } = new List<ConsultationDocument>();
}
