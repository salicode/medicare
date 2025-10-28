namespace MediCare.Models.DTOs.Doctors
{
    public record DoctorAvailabilityRequest(
        DayOfWeek DayOfWeek,
        TimeSpan StartTime,
        TimeSpan EndTime,
        bool IsRecurring = true,
        DateTime? SpecificDate = null,
        int MaxAppointmentsPerSlot = 1);
}