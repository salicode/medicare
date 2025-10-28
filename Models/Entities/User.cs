using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

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
}