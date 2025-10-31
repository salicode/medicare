
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models.Entities;
public class PatientRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    
    public List<TestResult> TestResults { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
    public List<Vital> Vitals { get; set; } = new();
    // public DateTime CreatedAt { get; internal set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}