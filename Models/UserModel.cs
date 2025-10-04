using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare
{
  


    public class UserRole
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public Guid? AssignedByUserId { get; set; }
    }



    // COMPLETELY NEW FILE - Junction table for roles and permissions
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }

    // COMPLETELY NEW FILE - Permission system
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

        [Required]
        // public Role Role { get; set; }

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

    // DTOs
    public record LoginRequest(string Username, string Password);
    public record LoginResponse(string Token, string Role, string Username);


    public record RegisterRequest(
        [Required][EmailAddress] string Email,
        [Required][MinLength(3)] string Username,
        [Required][MinLength(8)] string Password,
        [Required] Guid RoleId, 
        Guid? PatientProfileId = null);


    public record ResendConfirmationRequest([Required] string Email);
    public record ForgotPasswordRequest([Required][EmailAddress] string Email);
    public record ResetPasswordRequest([Required] string Token, [Required][MinLength(8)] string NewPassword);


    public record CreateRoleRequest(
    [Required][MaxLength(100)] string Name,
    [MaxLength(500)] string? Description,
    string[] Permissions);

    public record UpdateRoleRequest(
        [MaxLength(500)] string? Description,
        string[] Permissions);

    public record AssignRoleRequest(
        [Required] Guid UserId,
        [Required] Guid RoleId);

    public record RoleResponse(
        Guid Id,
        string Name,
        string? Description,
        bool IsSystemRole,
        DateTime CreatedAt,
        string[] Permissions);

    public record UserRoleResponse(
        Guid UserId,
        string Username,
        string Email,
        RoleResponse Role,
        DateTime AssignedAt);


}
