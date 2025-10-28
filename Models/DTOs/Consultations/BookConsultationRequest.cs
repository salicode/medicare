using System.ComponentModel.DataAnnotations;
using MediCare.MediCare;

namespace MediCare.Models.DTOs.Consultations
{
    public record BookConsultationRequest(
        [Required] Guid DoctorId,
        [Required] Guid PatientRecordId,
        [Required] DateTime ScheduledAt,
        ConsultationType ConsultationType,
        string? Symptoms);
}