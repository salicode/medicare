using System.ComponentModel.DataAnnotations;

using MediCare.Attributes;
namespace MediCare.Models.DTOs.Admin
{
    public record AdminCreateUserRequest(
        [Required][EmailAddress] string Email,
        [ValidInput(AllowedSpecialCharacters = "_-")]
        [Required][MinLength(3)] string Username,
        [Required][MinLength(8)] string Password,
        [Required] string Role, // "Doctor", "Nurse", "SuperAdmin"
        Guid? PatientProfileId = null);
}