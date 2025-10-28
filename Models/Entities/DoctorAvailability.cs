
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models.Entities;
public class DoctorAvailability
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    public bool IsRecurring { get; set; } = true;

    public DateTime? SpecificDate { get; set; } // For one-time availabilities

    public int MaxAppointmentsPerSlot { get; set; } = 1;

    // Navigation
    [ForeignKey("DoctorId")]
    public Doctor Doctor { get; set; } = null!;
}