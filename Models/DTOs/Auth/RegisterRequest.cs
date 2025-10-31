using System.ComponentModel.DataAnnotations;
using MediCare.Attributes;

namespace MediCare.Models.DTOs.Auth
{
    public record RegisterRequest(
        [Required][EmailAddress] string Email,
        [ValidInput(AllowedSpecialCharacters = "_-")] 
        [Required][MinLength(3)] string Username,
        [Required][MinLength(8)] string Password,
        Guid? PatientProfileId = null);
}