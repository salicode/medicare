namespace MediCare.Models.DTOs.Doctors
{
    public record UpdateDoctorProfileRequest(
        string? FullName,
        Guid? SpecializationId,
        string? PhoneNumber,
        string? Bio,
        int? YearsOfExperience,
        decimal? ConsultationFee);
}