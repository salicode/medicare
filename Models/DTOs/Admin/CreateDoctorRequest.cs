using System.ComponentModel.DataAnnotations;
using MediCare.Attributes;

namespace MediCare.Models.DTOs.Admin
{
    public record CreateDoctorRequest(
        [Required] Guid UserId,
        [ValidInput(AllowedSpecialCharacters = " .'-", ErrorMessage = "Full name can only contain letters, spaces, periods, apostrophes, and hyphens")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        [Required] string FullName,
        [Required] Guid SpecializationId,
        [ValidInput(AllowedSpecialCharacters = " +-().", AllowNull = true)]
        string? PhoneNumber,
        [ValidInput(AllowedSpecialCharacters = " .,!?@#$%&*+-/():;'", AllowNull = true)]
        [MaxLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        string? Bio,
        [Range(0, 60, ErrorMessage = "Years of experience must be between 0 and 60")]
        int YearsOfExperience,
        decimal ConsultationFee);
}