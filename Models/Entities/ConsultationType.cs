using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCare.Models.Entities;

public enum ConsultationType
{
    InPerson,
    Video,
    Phone
}