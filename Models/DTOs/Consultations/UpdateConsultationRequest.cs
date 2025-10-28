using MediCare.MediCare;


namespace MediCare.Models.DTOs.Consultations
{
    public record UpdateConsultationRequest(
        string? Diagnosis,
        string? TreatmentPlan,
        string? Notes,
        AppointmentStatus Status);
}