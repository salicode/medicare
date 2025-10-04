
using MediCare.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Models.Data;
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _db;

    public PermissionAuthorizationHandler(ApplicationDbContext db) => _db = db;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check if user has the required permission in database
        var userIdClaim = context.User.FindFirst("id")?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            return;

        var hasPermission = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.Permissions)
            .AnyAsync(rp => rp.Permission.Name == requirement.Permission);

        if (hasPermission)
            context.Succeed(requirement);
    }
}