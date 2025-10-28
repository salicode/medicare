using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Consultations
{
    public record AssignNurseRequest(
        [Required] Guid ConsultationId,
        [Required] Guid NurseId);
}