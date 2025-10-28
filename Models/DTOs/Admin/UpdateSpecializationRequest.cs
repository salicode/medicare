using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Admin
{
    public record UpdateSpecializationRequest(
        [Required][MaxLength(100)] string Name,
        [MaxLength(500)] string? Description);
}