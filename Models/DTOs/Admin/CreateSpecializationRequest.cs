using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Admin
{
    public record CreateSpecializationRequest(
        [Required][MaxLength(100)] string Name,
        [MaxLength(500)] string? Description);
}