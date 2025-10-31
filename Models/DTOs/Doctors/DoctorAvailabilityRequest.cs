using System.ComponentModel.DataAnnotations;
using MediCare.Helpers;
using MediCare.Attributes;
namespace MediCare.Models.DTOs.Doctors
{
    public record DoctorAvailabilityRequest(
        [Required(ErrorMessage = "Day of week is required")]
        DayOfWeek DayOfWeek,

        [Required(ErrorMessage = "Start time is required")]
        TimeSpan StartTime,

        [Required(ErrorMessage = "End time is required")]
        TimeSpan EndTime,
        bool IsRecurring = true,


        DateTime? SpecificDate = null,
        [Range(1, 10, ErrorMessage = "Maximum appointments per slot must be between 1 and 10")]
        int MaxAppointmentsPerSlot = 1);
}