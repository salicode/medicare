using System.Text.RegularExpressions;

namespace MediCare.Helpers
{
    public static class ValidationHelpers
    {
        // Basic input validation - allows only letters, digits, and specified special characters
        public static bool IsValidInput(string input, string allowedSpecialCharacters = "")
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var validSpecialChars = allowedSpecialCharacters.ToHashSet();
            
            return input.All(c => char.IsLetterOrDigit(c) || validSpecialChars.Contains(c));
        }

        // XSS prevention - checks for malicious scripts
        public static bool IsValidXSSInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            // Common XSS patterns to block
            var xssPatterns = new[]
            {
                @"<script", @"<iframe", @"<embed", @"<object", 
                @"javascript:", @"onload=", @"onerror=", @"onclick=",
                @"vbscript:", @"expression\s*\(", @"url\s*\("
            };

            var lowerInput = input.ToLowerInvariant();
            return !xssPatterns.Any(pattern => 
                Regex.IsMatch(lowerInput, pattern, RegexOptions.IgnoreCase));
        }

        // Sanitize input by removing potentially dangerous content
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove HTML tags and potentially dangerous content
            var sanitized = Regex.Replace(input, @"<[^>]*>", string.Empty);
            sanitized = Regex.Replace(sanitized, @"javascript:", string.Empty, RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"vbscript:", string.Empty, RegexOptions.IgnoreCase);
            
            return sanitized.Trim();
        }

        // Email validation
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email) && email.Length <= 254;
            }
            catch
            {
                return false;
            }
        }

        // Password strength validation
        public static bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            return password.Any(char.IsDigit) &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(c => !char.IsLetterOrDigit(c)); // Special character
        }

        // Phone number validation (basic)
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return true; // Optional field

            // Remove common separators and check if it's all digits
            var cleanNumber = Regex.Replace(phoneNumber, @"[\s\-\(\)\.]", "");
            return cleanNumber.All(char.IsDigit) && cleanNumber.Length >= 10;
        }

        // SQL Injection prevention - parameterized queries should be used, but this adds extra protection
        public static bool ContainsSqlInjectionPatterns(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var sqlPatterns = new[]
            {
                @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|EXEC|ALTER|CREATE|TRUNCATE)\b)",
                @"(\b(OR|AND)\b\s*\d+\s*=\s*\d+)",
                @"('|\-\-|;|\/\*)"
            };

            var upperInput = input.ToUpperInvariant();
            return sqlPatterns.Any(pattern => 
                Regex.IsMatch(upperInput, pattern, RegexOptions.IgnoreCase));
        }

        // Medical record specific validation
        public static bool IsValidMedicalText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            // Allow common medical abbreviations and symbols
            var allowedSpecialCharacters = " @#$%&*+-/.:;(),'";
            return input.All(c => char.IsLetterOrDigit(c) || 
                                 allowedSpecialCharacters.Contains(c) ||
                                 char.IsWhiteSpace(c));
        }

        // Consultation notes validation
        public static bool IsValidConsultationNotes(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            // More permissive for medical notes but still blocks XSS
            return IsValidXSSInput(input) && input.Length <= 1000; // Match your DB constraint
        }

        // File name validation for uploaded documents
        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var invalidChars = Path.GetInvalidFileNameChars();
            return !fileName.Any(invalidChars.Contains) &&
                   fileName.Length <= 255 && // Match your DB constraint
                   IsValidXSSInput(fileName);
        }
    }
}