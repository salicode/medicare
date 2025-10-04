

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MediCare.Models.Data
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<User> _hasher;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext db, 
            Microsoft.AspNetCore.Identity.IPasswordHasher<User> hasher,
            ILogger<AdminController> logger)
        {
            _db = db;
            _hasher = hasher;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<IActionResult> ListUsers()
        {
            try
            {
                var users = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Select(u => new 
                    { 
                        u.Id, 
                        u.Username, 
                        u.Email, 
                        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                        u.IsEmailConfirmed, 
                        u.CreatedAt 
                    })
                    .ToListAsync();
                    
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody]  RegisterRequest req) 
        {
            try
            {
                if (await _db.Users.AnyAsync(u => u.Username == req.Username))
                    return BadRequest("Username exists");

                if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                    return BadRequest("Email exists");

                if (!IsPasswordStrong(req.Password))
                    return BadRequest("Password does not meet strength requirements");

                // Validate role exists
                var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == req.RoleId);
                if (role == null)
                    return BadRequest("Invalid role");

                // Create user
                var user = new User 
                { 
                    Username = req.Username, 
                    Email = req.Email,
                    PatientProfileId = req.PatientProfileId,
                    IsEmailConfirmed = true // Admin-created users are auto-confirmed
                };
                
                user.PasswordHash = _hasher.HashPassword(user, req.Password);
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // Assign role to user
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = req.RoleId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedByUserId = Guid.Parse(User.FindFirst("id")!.Value) // Current admin user
                };
                _db.UserRoles.Add(userRole);
                await _db.SaveChangesAsync();
                
                _logger.LogInformation("Admin created user: {Username}", user.Username);
                
                return Ok(new { 
                    user.Id, 
                    user.Username, 
                    user.Email, 
                    Role = role.Name 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", req.Username);
                return StatusCode(500, "An error occurred while creating the user");
            }
        }

        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var user = await _db.Users
                    .Include(u => u.UserRoles) // ADDED: Include UserRoles for cleanup
                    .FirstOrDefaultAsync(u => u.Id == id);
                    
                if (user == null) 
                {
                    _logger.LogWarning("Attempt to delete non-existent user {UserId}", id);
                    return NotFound();
                }
                
                // Remove user roles first (due to foreign key constraints)
                _db.UserRoles.RemoveRange(user.UserRoles);
                
                // Then remove the user
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
                
                _logger.LogInformation("Admin deleted user: {Username}", user.Username);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        private bool IsPasswordStrong(string password)
        {
            return password.Length >= 8 && 
                   password.Any(char.IsDigit) && 
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower);
        }
    }
}
