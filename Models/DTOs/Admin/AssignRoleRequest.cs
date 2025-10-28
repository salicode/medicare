using System.ComponentModel.DataAnnotations;

namespace MediCare.Models.DTOs.Admin
{
    public record AssignRoleRequest(
        [Required] Guid UserId,
        [Required] Guid RoleId);
}