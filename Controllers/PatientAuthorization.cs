

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Models.Data
{
    public enum PatientAuthorizationOperation { View, Update, Prescribe }

    public class PatientAuthorizationRequirement : IAuthorizationRequirement
    {
        public PatientAuthorizationOperation Operation { get; }
        public PatientAuthorizationRequirement(PatientAuthorizationOperation op) => Operation = op;
    }

    public class PatientAuthorizationHandler : AuthorizationHandler<PatientAuthorizationRequirement, Guid>
    {
        private readonly ApplicationDbContext _db;
        public PatientAuthorizationHandler(ApplicationDbContext db) => _db = db;

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PatientAuthorizationRequirement requirement,
            Guid patientRecordId)
        {
            var userIdClaim = context.User.FindFirst("id")?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                return;

            var user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return;

            // Get user's roles
            var userRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            
            // SuperAdmin can do everything
            if (userRoles.Contains("SuperAdmin"))
            {
                context.Succeed(requirement);
                return;
            }

            // Check based on operation and roles
            switch (requirement.Operation)
            {
                case PatientAuthorizationOperation.View:
                    if (userRoles.Contains("Doctor") || userRoles.Contains("Nurse") || 
                        await IsPatientOwner(user, patientRecordId) || 
                        await IsAssignedToPatient(userId, patientRecordId))
                    {
                        context.Succeed(requirement);
                    }
                    break;

                case PatientAuthorizationOperation.Update:
                    if (userRoles.Contains("Doctor") || 
                        (userRoles.Contains("Nurse") && await IsAssignedToPatient(userId, patientRecordId)))
                    {
                        context.Succeed(requirement);
                    }
                    break;

                case PatientAuthorizationOperation.Prescribe:
                    if (userRoles.Contains("Doctor") && await IsAssignedToPatient(userId, patientRecordId))
                    {
                        context.Succeed(requirement);
                    }
                    break;
            }
        }

        private async Task<bool> IsPatientOwner(User user, Guid patientRecordId)
        {
            return user.PatientProfileId == patientRecordId;
        }

        private async Task<bool> IsAssignedToPatient(Guid userId, Guid patientRecordId)
        {
            return await _db.UserPatientAssignments
                .AnyAsync(a => a.UserId == userId && a.PatientRecordId == patientRecordId);
        }
    }
}
