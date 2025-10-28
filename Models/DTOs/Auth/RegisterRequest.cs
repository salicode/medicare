using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Auth
{
    public record RegisterRequest(
        [Required][EmailAddress] string Email,
        [Required][MinLength(3)] string Username,
        [Required][MinLength(8)] string Password,
        Guid? PatientProfileId = null);
}