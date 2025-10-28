using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Auth
{
    public record ForgotPasswordRequest([Required][EmailAddress] string Email);
}