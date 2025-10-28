using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Admin
{
    public record CreateDoctorRequest(
        [Required] Guid UserId,
        [Required] string FullName,
        [Required] Guid SpecializationId,
        string? PhoneNumber,
        string? Bio,
        int YearsOfExperience,
        decimal ConsultationFee);
}