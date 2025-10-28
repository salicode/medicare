namespace MediCare.Models.DTOs.Auth
{
    public record LoginResponse(string Token, string Role, string Username);
}