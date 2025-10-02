using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MediCare.Models.Data
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
                // Input validation
                if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
                    return BadRequest("Password must be at least 8 characters long");
                
                if (!IsPasswordStrong(req.Password))
                    return BadRequest("Password must contain at least one uppercase letter and one number");

                // Check for existing user
                if (await _db.Users.AnyAsync(u => u.Username == req.Username))
                    return BadRequest("Username already exists");

                if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                    return BadRequest("Email already exists");

                // Create user
                var user = new User
                {
                    Username = req.Username,
                    Email = req.Email,
                    Role = req.Role,
                    PatientProfileId = req.PatientProfileId,
                    IsEmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow
                };

                user.PasswordHash = _hasher.HashPassword(user, req.Password);
                
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // Generate email confirmation token
                var token = GenerateEmailConfirmationToken(user);
                _db.EmailConfirmationTokens.Add(token);
                await _db.SaveChangesAsync();

                // Send confirmation email
                var confirmationLink = Url.Action("ConfirmEmail", "Auth", 
                    new { userId = user.Id, token = token.Token }, Request.Scheme);
                
                await _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink);

                _logger.LogInformation("User registered successfully: {Username}", user.Username);
                
                return Ok(new { 
                    user.Id, 
                    user.Username, 
                    user.Email,
                    user.Role, 
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
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
                if (user == null) 
                {
                    _logger.LogWarning("Login attempt with non-existent username: {Username}", req.Username);
                    return Unauthorized("Invalid credentials");
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
                
                return Ok(new LoginResponse(token, user.Role.ToString(), user.Username));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Username}", req.Username);
                return StatusCode(500, "An error occurred during login");
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

            var claims = new List<Claim>
            {
                new Claim("id", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("email", user.Email),
                new Claim("role", user.Role.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("email_confirmed", user.IsEmailConfirmed.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
