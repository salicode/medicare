using System.ComponentModel.DataAnnotations;
using MediCare.Helpers;

namespace MediCare.Attributes
{
    public class ValidInputAttribute : ValidationAttribute
    {
        public string AllowedSpecialCharacters { get; set; } = "";
        public bool AllowNull { get; set; } = false;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return AllowNull ? ValidationResult.Success : new ValidationResult("Input cannot be null");

            if (value is string stringValue)
            {
                if (string.IsNullOrEmpty(stringValue))
                    return AllowNull ? ValidationResult.Success : new ValidationResult("Input cannot be empty");

                if (!ValidationHelpers.IsValidInput(stringValue, AllowedSpecialCharacters))
                    return new ValidationResult($"Input contains invalid characters. Only letters, digits, and {AllowedSpecialCharacters} are allowed.");

                if (ValidationHelpers.ContainsSqlInjectionPatterns(stringValue))
                    return new ValidationResult("Input contains potentially dangerous patterns.");

                if (!ValidationHelpers.IsValidXSSInput(stringValue))
                    return new ValidationResult("Input contains potentially dangerous content.");
            }

            return ValidationResult.Success;
        }
    }
}