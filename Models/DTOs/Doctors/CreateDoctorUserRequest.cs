using System;
using System.ComponentModel.DataAnnotations;
using MediCare.Helpers;
using MediCare.Attributes;
namespace MediCare.Models.DTOs.Doctors
{

    public record CreateDoctorUserRequest(

        [Required][EmailAddress] string Email,
        [Required][MinLength(3)] string Username,
        [Required][MinLength(8)] string Password,
   
        // Doctor profile fields
        [ValidInput(AllowedSpecialCharacters = " .'-", ErrorMessage = "Full name can only contain letters, spaces, periods, apostrophes, and hyphens")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        [Required] string FullName,
        [Required] Guid SpecializationId,
        [ValidInput(AllowedSpecialCharacters = " +-().", AllowNull = true, ErrorMessage = "Phone number contains invalid characters")]
        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        string? PhoneNumber,
        [ValidInput(AllowedSpecialCharacters = " .,!?@#$%&*+-/():;'", AllowNull = true, ErrorMessage = "Bio contains invalid characters")]
        [MaxLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        string? Bio,
        int YearsOfExperience,
        decimal ConsultationFee);
}