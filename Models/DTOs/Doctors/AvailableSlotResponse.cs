namespace MediCare.Models.DTOs.Doctors
{
    public record AvailableSlotResponse(
        DateTime StartTime,
        DateTime EndTime,
        bool IsAvailable,
        int BookedCount,
        int MaxAppointments);
}