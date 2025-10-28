namespace MediCare.Models.Entities
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
}