namespace MediCare.Models.DTOs.Admin
{
    public record SpecializationResponse(
        Guid Id,
        string Name,
        string? Description,
        DateTime CreatedAt);
}