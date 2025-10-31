using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MediCare.Helpers;
using MediCare.Attributes;
namespace MediCare.Models.Entities;

public class Doctor
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [ValidInput(AllowedSpecialCharacters = " .'-", ErrorMessage = "Full name can only contain letters, spaces, periods, apostrophes, and hyphens")]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [Required]
    public Guid SpecializationId { get; set; }
     
    [ValidInput(AllowedSpecialCharacters = " +-().", AllowNull = true)]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    [ValidInput(AllowedSpecialCharacters = " .,!?@#$%&*+-/():;'", AllowNull = true)]
    [MaxLength(500)]
    public string? Bio { get; set; }

    public int YearsOfExperience { get; set; }

    public decimal ConsultationFee { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("SpecializationId")]
    public Specialization Specialization { get; set; } = null!;

    public ICollection<DoctorAvailability> Availabilities { get; set; } = new List<DoctorAvailability>();
    public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
}