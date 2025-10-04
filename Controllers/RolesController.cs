using MediCare;
using MediCare.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;  
using Microsoft.Extensions.Logging;  

namespace MediCare.Models.Data  
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")] 
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RolesController> _logger;

        public RolesController(ApplicationDbContext db, ILogger<RolesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _db.Roles
                    .Include(r => r.Permissions)
                    .ThenInclude(rp => rp.Permission)
                    .Select(r => new RoleResponse(
                        r.Id,
                        r.Name,
                        r.Description,
                        r.IsSystemRole,
                        r.CreatedAt,
                        r.Permissions.Select(rp => rp.Permission.Name).ToArray()
                    ))
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, "An error occurred while retrieving roles");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetRole(Guid id)
        {
            try
            {
                var role = await _db.Roles
                    .Include(r => r.Permissions)
                    .ThenInclude(rp => rp.Permission)
                    .Where(r => r.Id == id)
                    .Select(r => new RoleResponse(
                        r.Id,
                        r.Name,
                        r.Description,
                        r.IsSystemRole,
                        r.CreatedAt,
                        r.Permissions.Select(rp => rp.Permission.Name).ToArray()
                    ))
                    .FirstOrDefaultAsync();

                if (role == null)
                    return NotFound();

                return Ok(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role {RoleId}", id);
                return StatusCode(500, "An error occurred while retrieving the role");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            try
            {
                // Check if role name already exists
                if (await _db.Roles.AnyAsync(r => r.Name == request.Name))
                    return BadRequest("Role name already exists");

                // Validate permissions
                var validPermissions = await _db.Permissions
                    .Where(p => request.Permissions.Contains(p.Name))
                    .ToListAsync();

                if (validPermissions.Count != request.Permissions.Length)
                    return BadRequest("One or more permissions are invalid");

                // Create role
                var role = new Role
                {
                    Name = request.Name,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Roles.Add(role);
                await _db.SaveChangesAsync(); 

                // Add permissions
                foreach (var permission in validPermissions)
                {
                    role.Permissions.Add(new RolePermission
                    {
                        PermissionId = permission.Id,
                        AssignedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync(); 

                _logger.LogInformation("Role created: {RoleName}", role.Name);

                var response = new RoleResponse(
                    role.Id,
                    role.Name,
                    role.Description,
                    role.IsSystemRole,
                    role.CreatedAt,
                    validPermissions.Select(p => p.Name).ToArray()
                );

                return CreatedAtAction(nameof(GetRole), new { id = role.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role {RoleName}", request.Name);
                return StatusCode(500, "An error occurred while creating the role");
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var role = await _db.Roles
                    .Include(r => r.Permissions)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (role == null)
                    return NotFound();

                if (role.IsSystemRole)
                    return BadRequest("System roles cannot be modified");

                // Update description
                if (!string.IsNullOrEmpty(request.Description))
                    role.Description = request.Description;

                role.UpdatedAt = DateTime.UtcNow;

                // Update permissions
                var validPermissions = await _db.Permissions
                    .Where(p => request.Permissions.Contains(p.Name))
                    .ToListAsync();

                if (validPermissions.Count != request.Permissions.Length)
                    return BadRequest("One or more permissions are invalid");

                // Clear existing permissions
                role.Permissions.Clear();

                // Add new permissions
                foreach (var permission in validPermissions)
                {
                    role.Permissions.Add(new RolePermission
                    {
                        PermissionId = permission.Id,
                        AssignedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Role updated: {RoleName}", role.Name);

                var response = new RoleResponse(
                    role.Id,
                    role.Name,
                    role.Description,
                    role.IsSystemRole,
                    role.CreatedAt,
                    validPermissions.Select(p => p.Name).ToArray()
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role {RoleId}", id);
                return StatusCode(500, "An error occurred while updating the role");
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            try
            {
                var role = await _db.Roles
                    .Include(r => r.UserRoles)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (role == null)
                    return NotFound();

                if (role.IsSystemRole)
                    return BadRequest("System roles cannot be deleted");

                if (role.UserRoles.Any())
                    return BadRequest("Cannot delete role that is assigned to users");

                _db.Roles.Remove(role);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Role deleted: {RoleName}", role.Name);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role {RoleId}", id);
                return StatusCode(500, "An error occurred while deleting the role");
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleRequest request)
        {
            try
            {
                var user = await _db.Users.FindAsync(request.UserId);
                var role = await _db.Roles.FindAsync(request.RoleId);

                if (user == null || role == null)
                    return NotFound("User or role not found");

                // Check if already assigned
                var existingAssignment = await _db.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId);

                if (existingAssignment != null)
                    return BadRequest("Role already assigned to user");

                var userRole = new UserRole
                {
                    UserId = request.UserId,
                    RoleId = request.RoleId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedByUserId = Guid.Parse(User.FindFirst("id")!.Value)
                };

                _db.UserRoles.Add(userRole);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Role {RoleId} assigned to user {UserId}", request.RoleId, request.UserId);

                return Ok(new { Message = "Role assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);
                return StatusCode(500, "An error occurred while assigning the role");
            }
        }

        [HttpDelete("unassign")]
        public async Task<IActionResult> UnassignRoleFromUser([FromBody] AssignRoleRequest request)
        {
            try
            {
                var userRole = await _db.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId);

                if (userRole == null)
                    return NotFound("Role assignment not found");

                _db.UserRoles.Remove(userRole);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Role {RoleId} unassigned from user {UserId}", request.RoleId, request.UserId);

                return Ok(new { Message = "Role unassigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning role {RoleId} from user {UserId}", request.RoleId, request.UserId);
                return StatusCode(500, "An error occurred while unassigning the role");
            }
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions()
        {
            try
            {
                var permissions = await _db.Permissions
                    .GroupBy(p => p.Category)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Select(p => new { p.Id, p.Name, p.Description }).ToList()
                    );

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions");
                return StatusCode(500, "An error occurred while retrieving permissions");
            }
        }
    }
}