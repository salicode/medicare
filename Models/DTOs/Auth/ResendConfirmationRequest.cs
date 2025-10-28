using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Auth
{
    public record ResendConfirmationRequest([Required] string Email);
}