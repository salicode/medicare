using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models.Entities;
public class ConsultationDocument
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ConsultationId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = null!;

    [MaxLength(100)]
    public string FileType { get; set; } = null!;

    public long FileSize { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid UploadedByUserId { get; set; }

    // Navigation
    [ForeignKey("ConsultationId")]
    public Consultation Consultation { get; set; } = null!;
}
