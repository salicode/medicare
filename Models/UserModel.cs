using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare
{
    public enum Role
    {
        SystemAdmin,
        Doctor,
        Nurse,
        Patient
    }

    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public Role Role { get; set; }

        // For patients: link to their patient profile id
        public Guid? PatientProfileId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public bool IsEmailConfirmed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }


    public class EmailConfirmationToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Token { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }

    public class PatientRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FullName { get; set; } = string.Empty;

        // Sensitive fields like test results, prescriptions, etc.
        public List<TestResult> TestResults { get; set; } = new();
        public List<Prescription> Prescriptions { get; set; } = new();
        public List<Vital> Vitals { get; set; } = new();
    }

    public class Vital
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // e.g., BP, Temp
        public string Value { get; set; } = string.Empty;
        public Guid PatientRecordId { get; set; }
    }

    public class TestResult
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Title { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public Guid PatientRecordId { get; set; }
    }

    public class Prescription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Medication { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public Guid PrescribedByUserId { get; set; } // doctor Id
        public Guid PatientRecordId { get; set; }
    }

    // Assignment linking healthcare staff to patient records
    public class UserPatientAssignment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }    // doctor or nurse
        public Guid PatientRecordId { get; set; }
    }

    // DTOs
    public record LoginRequest(string Username, string Password);
    public record LoginResponse(string Token, string Role, string Username);
    // public record RegisterRequest(string Username, string Password, Role Role, Guid? PatientProfileId = null);
    
    public record RegisterRequest(
        [Required][EmailAddress] string Email,
        [Required][MinLength(3)] string Username, 
        [Required][MinLength(8)] string Password, 
        [Required] Role Role, 
        Guid? PatientProfileId = null);

    
    public record ResendConfirmationRequest([Required] string Email);
    public record ForgotPasswordRequest([Required][EmailAddress] string Email);
    public record ResetPasswordRequest([Required] string Token, [Required][MinLength(8)] string NewPassword);

}
