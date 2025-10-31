using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MediCare.Helpers;
namespace MediCare.Models.Entities
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RoleConstants.SuperAdmin)]
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
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest req)
        {
            try
            {


                // Enhanced validation
                if (!ValidationHelpers.IsValidEmail(req.Email))
                    return BadRequest("Invalid email format");

                if (!ValidationHelpers.IsValidInput(req.Username, "_-"))
                    return BadRequest("Username contains invalid characters");

                if (!ValidationHelpers.IsPasswordStrong(req.Password))
                    return BadRequest("Password does not meet strength requirements");

                // SQL injection prevention
                if (ValidationHelpers.ContainsSqlInjectionPatterns(req.Username) ||
                    ValidationHelpers.ContainsSqlInjectionPatterns(req.Email) ||
                    ValidationHelpers.ContainsSqlInjectionPatterns(req.Role))
                    return BadRequest("Input contains potentially dangerous patterns");

                // XSS prevention
                if (!ValidationHelpers.IsValidXSSInput(req.Username) ||
                    !ValidationHelpers.IsValidXSSInput(req.Email))
                    return BadRequest("Input contains potentially dangerous content");

                // Sanitize inputs
                var sanitizedUsername = ValidationHelpers.SanitizeInput(req.Username);
                var sanitizedEmail = ValidationHelpers.SanitizeInput(req.Email);
                var sanitizedRole = ValidationHelpers.SanitizeInput(req.Role);

                // Check for existing user with sanitized data
                if (await _db.Users.AnyAsync(u => u.Username == sanitizedUsername))
                    return BadRequest("Username exists");

                if (await _db.Users.AnyAsync(u => u.Email == sanitizedEmail))
                    return BadRequest("Email exists");

                // Use sanitized data
                if (!RoleConstants.AllRoles.Contains(sanitizedRole))
                    return BadRequest($"Invalid role. Must be one of: {string.Join(", ", RoleConstants.AllRoles)}");

                // Continue with sanitized data...
                var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == sanitizedRole && r.IsSystemRole);

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

                // Assign role
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedByUserId = Guid.Parse(User.FindFirst("id")!.Value)
                };
                _db.UserRoles.Add(userRole);

                // If creating a doctor, also create a Doctor profile
                if (req.Role == RoleConstants.Doctor)
                {
                    var doctor = new Doctor
                    {
                        UserId = user.Id,
                        FullName = req.Username, // Default to username, can be updated later
                        SpecializationId = await GetDefaultSpecializationId(),
                        YearsOfExperience = 0,
                        ConsultationFee = 0,
                        IsActive = true
                    };
                    _db.Doctors.Add(doctor);
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin created user: {Username} with role: {Role}", user.Username, req.Role);

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    Role = role.Name,
                    user.PatientProfileId,
                    user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", req.Username);
                return StatusCode(500, "An error occurred while creating the user");
            }
        }

        [HttpPost("doctors")]
        public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorRequest req)
        {
            try
            {
                

                if (!ValidationHelpers.IsValidInput(req.FullName, " .'-"))
                    return BadRequest("Full name contains invalid characters. Only letters, spaces, periods, apostrophes, and hyphens are allowed.");

                if (!string.IsNullOrEmpty(req.PhoneNumber) && !ValidationHelpers.IsValidPhoneNumber(req.PhoneNumber))
                    return BadRequest("Invalid phone number format");

                if (!string.IsNullOrEmpty(req.Bio) && !ValidationHelpers.IsValidMedicalText(req.Bio))
                    return BadRequest("Bio contains invalid characters or potentially dangerous content");

                // XSS prevention
                if (!ValidationHelpers.IsValidXSSInput(req.FullName) ||
                    (!string.IsNullOrEmpty(req.PhoneNumber) && !ValidationHelpers.IsValidXSSInput(req.PhoneNumber)) ||
                    (!string.IsNullOrEmpty(req.Bio) && !ValidationHelpers.IsValidXSSInput(req.Bio)))
                    return BadRequest("Input contains potentially dangerous content");

                // SQL injection prevention
                if (ValidationHelpers.ContainsSqlInjectionPatterns(req.FullName) ||
                    (!string.IsNullOrEmpty(req.PhoneNumber) && ValidationHelpers.ContainsSqlInjectionPatterns(req.PhoneNumber)) ||
                    (!string.IsNullOrEmpty(req.Bio) && ValidationHelpers.ContainsSqlInjectionPatterns(req.Bio)))
                    return BadRequest("Input contains potentially dangerous patterns");

                if (req.YearsOfExperience < 0)
                    return BadRequest("Years of experience cannot be negative");

                if (req.ConsultationFee < 0)
                    return BadRequest("Consultation fee cannot be negative");

                if (req.YearsOfExperience > 60)
                    return BadRequest("Years of experience seems unrealistic");

                if (req.ConsultationFee > 10000) // $10,000 maximum fee
                    return BadRequest("Consultation fee is too high");

                // Sanitize inputs
                var sanitizedFullName = ValidationHelpers.SanitizeInput(req.FullName);
                var sanitizedPhoneNumber = !string.IsNullOrEmpty(req.PhoneNumber) ? ValidationHelpers.SanitizeInput(req.PhoneNumber) : null;
                var sanitizedBio = !string.IsNullOrEmpty(req.Bio) ? ValidationHelpers.SanitizeInput(req.Bio) : null;

                // Verify user exists and has Doctor role
                var user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == req.UserId);

                if (user == null)
                {
                    _logger.LogWarning("Attempt to create doctor profile for non-existent user: {UserId}", req.UserId);
                    return NotFound("User not found");
                }

                if (!user.UserRoles.Any(ur => ur.Role.Name == RoleConstants.Doctor))
                {
                    _logger.LogWarning("Attempt to create doctor profile for user without Doctor role: {UserId}", req.UserId);
                    return BadRequest("User does not have Doctor role");
                }

                // Check if doctor profile already exists
                if (await _db.Doctors.AnyAsync(d => d.UserId == req.UserId))
                {
                    _logger.LogWarning("Attempt to create duplicate doctor profile for user: {UserId}", req.UserId);
                    return BadRequest("Doctor profile already exists for this user");
                }

                // Verify specialization exists and is valid
                var specialization = await _db.Specializations.FindAsync(req.SpecializationId);
                if (specialization == null)
                {
                    _logger.LogWarning("Attempt to create doctor with invalid specialization: {SpecializationId}", req.SpecializationId);
                    return BadRequest("Invalid specialization");
                }

                // Check if full name is already in use by another doctor
                if (await _db.Doctors.AnyAsync(d => d.FullName == sanitizedFullName && d.UserId != req.UserId))
                {
                    _logger.LogWarning("Attempt to create doctor with duplicate full name: {FullName}", sanitizedFullName);
                    return BadRequest("A doctor with this full name already exists");
                }

                var doctor = new Doctor
                {
                    UserId = req.UserId,
                    FullName = req.FullName,
                    SpecializationId = req.SpecializationId,
                    PhoneNumber = req.PhoneNumber,
                    Bio = req.Bio,
                    YearsOfExperience = req.YearsOfExperience,
                    ConsultationFee = req.ConsultationFee,
                    IsActive = true
                };

                _db.Doctors.Add(doctor);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin created doctor profile for user: {UserId}", req.UserId);

                return Ok(new
                {
                    doctor.Id,
                    doctor.FullName,
                    Specialization = specialization.Name,
                    doctor.YearsOfExperience,
                    doctor.ConsultationFee
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating doctor profile for user {UserId}", req.UserId);
                return StatusCode(500, "An error occurred while creating doctor profile");
            }
        }

        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors()
        {
            try
            {
                var doctors = await _db.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Specialization)
                    .Select(d => new
                    {
                        d.Id,
                        d.FullName,
                        d.YearsOfExperience,
                        d.ConsultationFee,
                        d.Bio,
                        d.PhoneNumber,
                        d.IsActive,
                        Specialization = d.Specialization.Name,
                        Username = d.User.Username,
                        Email = d.User.Email,
                        UserId = d.UserId
                    })
                    .ToListAsync();

                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctors");
                return StatusCode(500, "An error occurred while retrieving doctors");
            }
        }

        [HttpGet("nurses")]
        public async Task<IActionResult> GetNurses()
        {
            try
            {
                var nurses = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == RoleConstants.Nurse))
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.IsEmailConfirmed,
                        u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(nurses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving nurses");
                return StatusCode(500, "An error occurred while retrieving nurses");
            }
        }

        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var user = await _db.Users
                    .Include(u => u.UserRoles)
                    .Include(u => u.PatientProfileId.HasValue ? u : null)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    _logger.LogWarning("Attempt to delete non-existent user {UserId}", id);
                    return NotFound();
                }

                // Remove doctor profile if exists
                var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == id);
                if (doctor != null)
                {
                    // Remove doctor availabilities first
                    var availabilities = await _db.DoctorAvailabilities
                        .Where(da => da.DoctorId == doctor.Id)
                        .ToListAsync();
                    _db.DoctorAvailabilities.RemoveRange(availabilities);

                    _db.Doctors.Remove(doctor);
                }

                // Remove user roles first (due to foreign key constraints)
                _db.UserRoles.RemoveRange(user.UserRoles);

                // Remove user-patient assignments
                var assignments = await _db.UserPatientAssignments
                    .Where(a => a.UserId == id)
                    .ToListAsync();
                _db.UserPatientAssignments.RemoveRange(assignments);

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

        private async Task<Guid> GetDefaultSpecializationId()
        {
            var defaultSpecialization = await _db.Specializations
                .FirstOrDefaultAsync(s => s.Name == "General Practice");

            if (defaultSpecialization != null)
                return defaultSpecialization.Id;

            // Create default specialization if none exists
            var specialization = new Specialization
            {
                Name = "General Practice",
                Description = "Primary care and general medicine"
            };
            _db.Specializations.Add(specialization);
            await _db.SaveChangesAsync();

            return specialization.Id;
        }

        // Add these methods to your existing AdminController class

        [HttpPost("specializations")]
        public async Task<IActionResult> CreateSpecialization([FromBody] CreateSpecializationRequest request)
        {
            try
            {
                if (await _db.Specializations.AnyAsync(s => s.Name == request.Name))
                    return BadRequest("Specialization with this name already exists");

                var specialization = new Specialization
                {
                    Name = request.Name,
                    Description = request.Description
                };

                _db.Specializations.Add(specialization);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin created specialization: {SpecializationName}", request.Name);

                return Ok(new SpecializationResponse(
                    specialization.Id,
                    specialization.Name,
                    specialization.Description,
                    specialization.CreatedAt
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating specialization {SpecializationName}", request.Name);
                return StatusCode(500, "An error occurred while creating specialization");
            }
        }

        [HttpGet("specializations")]
        public async Task<IActionResult> GetSpecializations()
        {
            try
            {
                var specializations = await _db.Specializations
                    .OrderBy(s => s.Name)
                    .Select(s => new SpecializationResponse(
                        s.Id,
                        s.Name,
                        s.Description,
                        s.CreatedAt
                    ))
                    .ToListAsync();

                return Ok(specializations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving specializations");
                return StatusCode(500, "An error occurred while retrieving specializations");
            }
        }

        [HttpPut("specializations/{id:guid}")]
        public async Task<IActionResult> UpdateSpecialization(Guid id, [FromBody] UpdateSpecializationRequest request)
        {
            try
            {
                var specialization = await _db.Specializations.FindAsync(id);
                if (specialization == null)
                    return NotFound("Specialization not found");

                if (await _db.Specializations.AnyAsync(s => s.Name == request.Name && s.Id != id))
                    return BadRequest("Another specialization with this name already exists");

                specialization.Name = request.Name;
                specialization.Description = request.Description;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin updated specialization: {SpecializationName}", request.Name);

                return Ok(new SpecializationResponse(
                    specialization.Id,
                    specialization.Name,
                    specialization.Description,
                    specialization.CreatedAt
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating specialization {SpecializationId}", id);
                return StatusCode(500, "An error occurred while updating specialization");
            }
        }

        [HttpDelete("specializations/{id:guid}")]
        public async Task<IActionResult> DeleteSpecialization(Guid id)
        {
            try
            {
                var specialization = await _db.Specializations
                    .Include(s => s.Doctors)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (specialization == null)
                    return NotFound("Specialization not found");

                if (specialization.Doctors.Any())
                    return BadRequest("Cannot delete specialization that has doctors assigned to it");

                _db.Specializations.Remove(specialization);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin deleted specialization: {SpecializationName}", specialization.Name);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting specialization {SpecializationId}", id);
                return StatusCode(500, "An error occurred while deleting specialization");
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
