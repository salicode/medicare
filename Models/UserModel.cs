using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MediCare.MediCare;

namespace MediCare
{


    public static class RoleConstants
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Doctor = "Doctor";
        public const string Nurse = "Nurse";
        public const string Patient = "Patient";

        public static readonly string[] AllRoles = { SuperAdmin, Doctor, Nurse, Patient };
        public static readonly string[] StaffRoles = { SuperAdmin, Doctor, Nurse };
    }

    public class SystemRoleConfig
    {
        public string Id { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsSystemRole { get; set; } = true;
    }
    public class UserRole
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public Guid? AssignedByUserId { get; set; }
    }


    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }


    public class Permission
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = null!;

        // Navigation properties
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class Role
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsSystemRole { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;


        [Compare("PasswordHash", ErrorMessage = "Passwords do not match")]

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public bool HasPermission(string permissionName)
        {
            return UserRoles.Any(ur =>
                ur.Role.Permissions.Any(rp =>
                    rp.Permission.Name == permissionName));
        }

        public bool HasRole(string roleName)
        {
            return UserRoles.Any(ur => ur.Role.Name == roleName);
        }

        public IEnumerable<string> GetPermissions()
        {
            return UserRoles
                .SelectMany(ur => ur.Role.Permissions)
                .Select(rp => rp.Permission.Name)
                .Distinct();
        }

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

    // Add to MediCare namespace
namespace MediCare
{
    public enum AppointmentStatus
    {
        Pending,
        Confirmed,
        InProgress,
        Completed,
        Cancelled,
        NoShow
    }

    public enum ConsultationType
    {
        InPerson,
        Video,
        Phone
    }

    public class Consultation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid PatientRecordId { get; set; }
        
        [Required]
        public Guid DoctorId { get; set; }
        
        public Guid? NurseId { get; set; } // Assigned nurse
        
        [Required]
        public DateTime ScheduledAt { get; set; }
        
        [Required]
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);
        
        [Required]
        public ConsultationType ConsultationType { get; set; }
        
        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        
        [MaxLength(1000)]
        public string? Symptoms { get; set; }
        
        [MaxLength(1000)]
        public string? Diagnosis { get; set; }
        
        [MaxLength(1000)]
        public string? TreatmentPlan { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public decimal Fee { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("PatientRecordId")]
        public PatientRecord PatientRecord { get; set; } = null!;
        
        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; } = null!;
        
        [ForeignKey("NurseId")]
        public User? Nurse { get; set; }
        
        public ICollection<ConsultationDocument> Documents { get; set; } = new List<ConsultationDocument>();
    }

    public class ConsultationDocument
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid ConsultationId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;
        
        [MaxLength(100)]
        public string FileType { get; set; } = null!;
        
        public long FileSize { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public Guid UploadedByUserId { get; set; }
        
        // Navigation
        [ForeignKey("ConsultationId")]
        public Consultation Consultation { get; set; } = null!;
    }
}


     public class Specialization
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }

    public class Doctor
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;
        
        [Required]
        public Guid SpecializationId { get; set; }
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        [MaxLength(500)]
        public string? Bio { get; set; }
        
        public int YearsOfExperience { get; set; }
        
        public decimal ConsultationFee { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        
        [ForeignKey("SpecializationId")]
        public Specialization Specialization { get; set; } = null!;
        
        public ICollection<DoctorAvailability> Availabilities { get; set; } = new List<DoctorAvailability>();
        public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
    }

    public class DoctorAvailability
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid DoctorId { get; set; }
        
        [Required]
        public DayOfWeek DayOfWeek { get; set; }
        
        [Required]
        public TimeSpan StartTime { get; set; }
        
        [Required]
        public TimeSpan EndTime { get; set; }
        
        public bool IsRecurring { get; set; } = true;
        
        public DateTime? SpecificDate { get; set; } // For one-time availabilities
        
        public int MaxAppointmentsPerSlot { get; set; } = 1;
        
        // Navigation
        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; } = null!;
    }

    // DTOs
    public record LoginRequest(string Username, string Password);
    public record LoginResponse(string Token, string Role, string Username);


    // For patient self-registration
    public record RegisterRequest(
        [Required][EmailAddress] string Email,
        [Required][MinLength(3)] string Username,
        [Required][MinLength(8)] string Password,
        Guid? PatientProfileId = null);


    // For admin creating staff users  
    public record AdminCreateUserRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(3)] string Username,
    [Required][MinLength(8)] string Password,
    [Required] string Role, // "Doctor", "Nurse", "SuperAdmin"
    Guid? PatientProfileId = null);


    public record ResendConfirmationRequest([Required] string Email);
    public record ForgotPasswordRequest([Required][EmailAddress] string Email);
    public record ResetPasswordRequest([Required] string Token, [Required][MinLength(8)] string NewPassword);



    public record AssignRoleRequest(
        [Required] Guid UserId,
        [Required] Guid RoleId);

    public record UserRoleResponse(
        Guid UserId,
        string Username,
        string Email,
        DateTime AssignedAt);


      public record CreateDoctorRequest(
        [Required] Guid UserId,
        [Required] string FullName,
        [Required] Guid SpecializationId,
        string? PhoneNumber,
        string? Bio,
        int YearsOfExperience,
        decimal ConsultationFee);

    public record DoctorAvailabilityRequest(
        DayOfWeek DayOfWeek,
        TimeSpan StartTime,
        TimeSpan EndTime,
        bool IsRecurring = true,
        DateTime? SpecificDate = null,
        int MaxAppointmentsPerSlot = 1);

    public record BookConsultationRequest(
        [Required] Guid DoctorId,
        [Required] Guid PatientRecordId,
        [Required] DateTime ScheduledAt,
        ConsultationType ConsultationType,
        string? Symptoms);

    public record AssignNurseRequest(
        [Required] Guid ConsultationId,
        [Required] Guid NurseId);

    public record UpdateConsultationRequest(
        string? Diagnosis,
        string? TreatmentPlan,
        string? Notes,
        AppointmentStatus Status);

    public record AvailableSlotResponse(
        DateTime StartTime,
        DateTime EndTime,
        bool IsAvailable,
        int BookedCount,
        int MaxAppointments);

    public record UpdateConsultationStatusRequest(AppointmentStatus Status);


public record CreateSpecializationRequest(
    [Required][MaxLength(100)] string Name,
    [MaxLength(500)] string? Description);

public record UpdateSpecializationRequest(
    [Required][MaxLength(100)] string Name,
    [MaxLength(500)] string? Description);

public record SpecializationResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt);



}
