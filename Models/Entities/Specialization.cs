using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MediCare.Helpers;
using MediCare.Attributes;
namespace MediCare.Models.Entities;

public class Specialization
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();


    [Required(ErrorMessage = "Specialization name is required")]
    [MaxLength(100, ErrorMessage = "Specialization name cannot exceed 100 characters")]
    [ValidInput(AllowedSpecialCharacters = " &-", ErrorMessage = "Specialization name can only contain letters, digits, spaces, ampersands, and hyphens")]
    public string Name { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [ValidInput(AllowedSpecialCharacters = " .,!?@#$%&*+-/():;'", AllowNull = true, ErrorMessage = "Description contains invalid characters")]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}