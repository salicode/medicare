
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models.Entities;
public class UserPatientAssignment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }    // doctor or nurse
    public Guid PatientRecordId { get; set; }
}