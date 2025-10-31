using System.ComponentModel.DataAnnotations;
using MediCare.Helpers;
using MediCare.Attributes;
namespace MediCare.Models.DTOs.Admin
{
    public record CreateSpecializationRequest(
        [Required(ErrorMessage = "Specialization name is required")]
        [MaxLength(100, ErrorMessage = "Specialization name cannot exceed 100 characters")]
        [ValidInput(AllowedSpecialCharacters = " &-", ErrorMessage = "Specialization name can only contain letters, digits, spaces, ampersands, and hyphens")]
        string Name,
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [ValidInput(AllowedSpecialCharacters = " .,!?@#$%&*+-/():;'", AllowNull = true, ErrorMessage = "Description contains invalid characters")]
        string? Description);
}