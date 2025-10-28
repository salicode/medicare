using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Auth
{
    public record ResetPasswordRequest([Required] string Token, [Required][MinLength(8)] string NewPassword);
}