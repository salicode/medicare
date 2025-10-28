namespace MediCare.Models.DTOs.Admin
{
    public record UserRoleResponse(
        Guid UserId,
        string Username,
        string Email,
        DateTime AssignedAt);
}