using Microsoft.AspNetCore.Authorization;

using  MediCare;

using Microsoft.AspNetCore.Authorization;

namespace MediCare.Models.Data
{
    public enum PatientAuthorizationOperation { View, Update, Prescribe }

    // Requirement carries the operation the policy checks for
    public class PatientAuthorizationRequirement : IAuthorizationRequirement
    {
        public PatientAuthorizationOperation Operation { get; }
        public PatientAuthorizationRequirement(PatientAuthorizationOperation op) => Operation = op;
    }

    // Handler that checks the user role and assignments.
    public class PatientAuthorizationHandler : AuthorizationHandler<PatientAuthorizationRequirement, Guid>
    {
        private readonly ApplicationDbContext _db;
        public PatientAuthorizationHandler(ApplicationDbContext db) => _db = db;

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PatientAuthorizationRequirement requirement, Guid resource)
        {
            // resource is patientRecordId (Guid)
            var sub = context.User.FindFirst("id")?.Value;
            var roleClaim = context.User.FindFirst("role")?.Value;

            if (sub == null || roleClaim == null)
                return Task.CompletedTask;

            if (!Guid.TryParse(sub, out var userId))
                return Task.CompletedTask;

            // If admin -> success
            if (Enum.TryParse<Role>(roleClaim, out var role) && role == Role.SystemAdmin)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // If patient -> must be own profile for View operation
            if (role == Role.Patient)
            {
                var user = _db.Users.Find(userId);
                if (user?.PatientProfileId == resource && requirement.Operation == PatientAuthorizationOperation.View)
                {
                    context.Succeed(requirement);
                }
                return Task.CompletedTask;
            }

            // For Doctor/Nurse -> check assignment
            if (role == Role.Doctor || role == Role.Nurse)
            {
                var assigned = _db.UserPatientAssignments.Any(a => a.UserId == userId && a.PatientRecordId == resource);
                if (!assigned) return Task.CompletedTask;

                // role-specific operation restrictions:
                if (role == Role.Nurse)
                {
                    // Nurse can view and update vitals only (we map Update to vitals updates at controller level)
                    if (requirement.Operation == PatientAuthorizationOperation.View || requirement.Operation == PatientAuthorizationOperation.Update)
                    {
                        context.Succeed(requirement);
                    }
                }
                else if (role == Role.Doctor)
                {
                    // Doctor may view/update/prescribe
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
