using MediCare.Helpers;
using MediCare.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Doctors
{
    public record UpdateDoctorProfileRequest(
        [ValidInput(AllowedSpecialCharacters = " .'-", ErrorMessage = "Full name can only contain letters, spaces, periods, apostrophes, and hyphens")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        string? FullName,
        Guid? SpecializationId,

        [ValidInput(AllowedSpecialCharacters = " +-().", AllowNull = true, ErrorMessage = "Phone number contains invalid characters")]
        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        string? PhoneNumber,

        [ValidInput(AllowedSpecialCharacters = " .,!?@#$%&*+-/():;'", AllowNull = true, ErrorMessage = "Bio contains invalid characters")]
        [MaxLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        string? Bio,
        [Range(0, 60, ErrorMessage = "Years of experience must be between 0 and 60")]
        int? YearsOfExperience,
        decimal? ConsultationFee);
}