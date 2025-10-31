using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediCare.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace MediCare.Models.Entities
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<User> _hasher;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext db,
            Microsoft.AspNetCore.Identity.IPasswordHasher<User> hasher,
            IConfiguration config,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _db = db;
            _hasher = hasher;
            _config = config;
            _emailService = emailService;
            _logger = logger;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            try
            {

                if (!ValidationHelpers.IsValidEmail(req.Email))
                    return BadRequest("Invalid email format");

                if (!ValidationHelpers.IsValidInput(req.Username, "_-"))
                    return BadRequest("Username contains invalid characters");

                if (!ValidationHelpers.IsPasswordStrong(req.Password))
                    return BadRequest("Password must be at least 8 characters with uppercase, lowercase, number, and special character");

                if (ValidationHelpers.ContainsSqlInjectionPatterns(req.Username) ||
                    ValidationHelpers.ContainsSqlInjectionPatterns(req.Email))
                    return BadRequest("Input contains invalid patterns");

                // Sanitize inputs
                var sanitizedUsername = ValidationHelpers.SanitizeInput(req.Username);
                var sanitizedEmail = ValidationHelpers.SanitizeInput(req.Email);


                if (await _db.Users.AnyAsync(u => u.Username == sanitizedUsername))
                    return BadRequest("Username exists");

                if (await _db.Users.AnyAsync(u => u.Email == sanitizedEmail))
                    return BadRequest("Email exists");


                // Use constant for default role
                var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == RoleConstants.Patient);


                if (defaultRole == null)
                    return BadRequest("Default role not found");

                // Create user
                var user = new User
                {
                    // Username = req.Username,
                    // Email = req.Email,
                    Username = sanitizedUsername,
                    Email = sanitizedEmail,
                    PatientProfileId = req.PatientProfileId,
                    IsEmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow
                };

                user.PasswordHash = _hasher.HashPassword(user, req.Password);
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // Assign default role
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = defaultRole.Id,
                    AssignedAt = DateTime.UtcNow
                };

                _db.UserRoles.Add(userRole);

                // Generate email confirmation token
                var token = GenerateEmailConfirmationToken(user);
                _db.EmailConfirmationTokens.Add(token);
                await _db.SaveChangesAsync();

                // Send confirmation email
                var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                    new { userId = user.Id, token = token.Token }, Request.Scheme);

                await _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink);

                _logger.LogInformation("User registered successfully: {Username}", user.Username);

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    Role = defaultRole.Name,
                    PatientRecordId = user.PatientProfileId, 
                    RequiresConfirmation = true,
                    Message = "Registration successful. Please check your email for confirmation instructions."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Username}", req.Username);
                return StatusCode(500, "An error occurred during registration");
            }
        }






        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            try


            {
                if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                {
                    _logger.LogWarning("Login attempt with empty username or password");
                    return Unauthorized("Invalid credentials");
                }

                // Validate username format
                if (!ValidationHelpers.IsValidInput(req.Username, "_-"))
                {
                    _logger.LogWarning("Login attempt with invalid username format: {Username}", req.Username);
                    return Unauthorized("Invalid credentials");
                }

                // Validate password contains only allowed characters
                if (!ValidationHelpers.IsValidInput(req.Password, "!@#$%^&*()_+-=[]{}|;:,.<>?"))
                {
                    _logger.LogWarning("Login attempt with invalid password characters for user: {Username}", req.Username);
                    return Unauthorized("Invalid credentials");
                }

                // SQL injection prevention
                if (ValidationHelpers.ContainsSqlInjectionPatterns(req.Username))
                {
                    _logger.LogWarning("Potential SQL injection attempt in login username: {Username}", req.Username);
                    return Unauthorized("Invalid credentials");
                }

                // XSS prevention
                if (!ValidationHelpers.IsValidXSSInput(req.Username))
                {
                    _logger.LogWarning("Potential XSS attempt in login username: {Username}", req.Username);
                    return Unauthorized("Invalid credentials");
                }





                var sanitizedUsername = ValidationHelpers.SanitizeInput(req.Username);

                var user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Username == sanitizedUsername);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt with non-existent username: {Username}", req.Username);
                    return Unauthorized("Invalid credentials");
                }

                // DEBUG: Check what roles the user actually has
                var userRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
                Console.WriteLine($"=== DEBUG LOGIN ===");
                Console.WriteLine($"User: {user.Username}");
                Console.WriteLine($"Roles from database: {string.Join(", ", userRoles)}");
                Console.WriteLine($"Role count: {userRoles.Count}");

                foreach (var userRole in user.UserRoles)
                {
                    Console.WriteLine($"Role ID: {userRole.RoleId}, Role Name: '{userRole.Role.Name}'");
                }

                // Check if email is confirmed
                if (!user.IsEmailConfirmed)
                {
                    _logger.LogWarning("Login attempt with unconfirmed email: {Username}", req.Username);
                    return Unauthorized("Please confirm your email before logging in");
                }

                var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
                if (verify == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                {
                    _logger.LogWarning("Invalid password attempt for user: {Username}", req.Username);
                    return Unauthorized("Invalid credentials");
                }

                var token = GenerateJwt(user);

                _logger.LogInformation("User logged in successfully: {Username}", user.Username);

                // FIXED: Use the same primary role logic as JWT generation
                var primaryRole = GetPrimaryRole(userRoles);

                Console.WriteLine($"Selected primary role: {primaryRole}");
                Console.WriteLine($"=== END DEBUG ===");

                return Ok(new LoginResponse(token, primaryRole, user.Username));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Username}", req.Username);
                return StatusCode(500, "An error occurred during login");
            }
        }


        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);

                var user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.PatientProfileId,
                        u.IsEmailConfirmed,
                        u.CreatedAt,
                        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                        
                    })
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);

                if (user == null)
                    return NotFound("User not found");

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user info");
                return StatusCode(500, "An error occurred while retrieving user information");
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
        {
            try
            {
                var confirmationToken = await _db.EmailConfirmationTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t =>
                        t.UserId == userId &&
                        t.Token == token &&
                        t.UsedAt == null);

                if (confirmationToken == null)
                {
                    _logger.LogWarning("Invalid confirmation token attempt for user {UserId}", userId);
                    return BadRequest("Invalid confirmation token");
                }

                if (confirmationToken.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Expired confirmation token for user {UserId}", userId);
                    return BadRequest("Confirmation token has expired");
                }

                confirmationToken.User.IsEmailConfirmed = true;
                confirmationToken.UsedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Email confirmed successfully for user {UserId}", userId);

                return Ok(new { Message = "Email confirmed successfully. You can now log in." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
                return StatusCode(500, "An error occurred while confirming email");
            }
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    // Don't reveal that email doesn't exist
                    return Ok(new { Message = "If the email exists, a confirmation link has been sent." });
                }

                if (user.IsEmailConfirmed)
                {
                    return BadRequest("Email is already confirmed");
                }

                // Invalidate existing tokens
                var existingTokens = _db.EmailConfirmationTokens
                    .Where(t => t.UserId == user.Id && t.UsedAt == null);
                await existingTokens.ForEachAsync(t => t.UsedAt = DateTime.UtcNow);

                // Generate new token
                var newToken = GenerateEmailConfirmationToken(user);
                _db.EmailConfirmationTokens.Add(newToken);
                await _db.SaveChangesAsync();

                // Send confirmation email
                var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                    new { userId = user.Id, token = newToken.Token }, Request.Scheme);

                await _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink);

                _logger.LogInformation("Confirmation email resent for {Email}", user.Email);

                return Ok(new { Message = "Confirmation email has been resent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending confirmation email for {Email}", request.Email);
                return StatusCode(500, "An error occurred while resending confirmation email");
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    // Don't reveal that email doesn't exist
                    return Ok(new { Message = "If the email exists, a password reset link has been sent." });
                }

                // Generate password reset token (similar to email confirmation)
                var resetToken = GeneratePasswordResetToken(user);
                _db.EmailConfirmationTokens.Add(resetToken);
                await _db.SaveChangesAsync();

                // Send password reset email
                var resetLink = Url.Action("ResetPassword", "Auth",
                    new { token = resetToken.Token }, Request.Scheme);

                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

                _logger.LogInformation("Password reset email sent for {Email}", user.Email);

                return Ok(new { Message = "If the email exists, a password reset link has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for {Email}", request.Email);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        private bool IsPasswordStrong(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsDigit) &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower);
        }

        private EmailConfirmationToken GenerateEmailConfirmationToken(User user)
        {
            return new EmailConfirmationToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "_").Replace("+", "-"),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        private EmailConfirmationToken GeneratePasswordResetToken(User user)
        {
            return new EmailConfirmationToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "_").Replace("+", "-"),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(2) // Shorter expiry for password reset
            };
        }


        private string GenerateJwt(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Get user roles
            var userRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // FIXED: Get the highest priority role instead of first role
            var primaryRole = GetPrimaryRole(userRoles);

            var claims = new List<Claim>
    {
        new Claim("id", user.Id.ToString()),
        new Claim("username", user.Username),
        new Claim("email", user.Email),
        new Claim("primary_role", primaryRole),
        new Claim("email_confirmed", user.IsEmailConfirmed.ToString())
    };

            // Add all roles as claims (for [Authorize(Roles = "X")] to work)
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ADD THIS HELPER METHOD
        private string GetPrimaryRole(List<string> roles)
        {
            // Define role priority (highest to lowest)
            var rolePriority = new Dictionary<string, int>
    {
        { RoleConstants.SuperAdmin, 4 },
        { RoleConstants.Doctor, 3 },
        { RoleConstants.Nurse, 2 },
        { RoleConstants.Patient, 1 }
    };

            // Return the highest priority role
            return roles.OrderByDescending(r => rolePriority.GetValueOrDefault(r, 0))
                        .FirstOrDefault() ?? RoleConstants.Patient;
        }
    }
}
