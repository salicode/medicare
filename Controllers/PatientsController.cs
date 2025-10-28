using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace MediCare.Models.Entities
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthorizationService _auth;

        public PatientsController(ApplicationDbContext db, IAuthorizationService auth)
        {
            _db = db;
            _auth = auth;
        }

        // View patient record (sensitive fields included)
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "CanViewPatient")]
        public async Task<IActionResult> GetPatient(Guid id)
        {
            // Authorization handler uses the resource (patient id) to check assignments
            var authResult = await _auth.AuthorizeAsync(User, id, "CanViewPatient");
            if (!authResult.Succeeded) return Forbid();

            var patient = await _db.PatientRecords
                .Include(p => p.TestResults)
                .Include(p => p.Prescriptions)
                .Include(p => p.Vitals)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            return Ok(patient);
        }

        // Update patient general info (doctor only / admin) - uses CanUpdatePatient
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanUpdatePatient")]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] PatientRecord update)
        {
            var authResult = await _auth.AuthorizeAsync(User, id, "CanUpdatePatient");
            if (!authResult.Succeeded) return Forbid();

            var patient = await _db.PatientRecords.FindAsync(id);
            if (patient == null) return NotFound();

            patient.FullName = update.FullName;
            await _db.SaveChangesAsync();
            return Ok(patient);
        }



        [HttpPost("{id:guid}/vitals")]
        [Authorize(Roles = $"{RoleConstants.Doctor},{RoleConstants.Nurse},{RoleConstants.SuperAdmin}")]
        public async Task<IActionResult> AddVital(Guid id, [FromBody] Vital vital)
        {
            // Ensure patient exists
            var patient = await _db.PatientRecords.FindAsync(id);
            if (patient == null) return NotFound();

            // Check if user is assigned to this patient
            var userId = Guid.Parse(User.FindFirst("id")!.Value);
            var isAssigned = await _db.UserPatientAssignments
                .AnyAsync(a => a.UserId == userId && a.PatientRecordId == id);

            // Use constant for role check
            if (!isAssigned && !User.IsInRole(RoleConstants.SuperAdmin))
                return Forbid();

            vital.PatientRecordId = id;
            _db.Vitals.Add(vital);
            await _db.SaveChangesAsync();
            return Ok(vital);
        }

        // Doctor prescribes medication
        [HttpPost("{id:guid}/prescriptions")]
        [Authorize(Policy = "CanPrescribe")]
        public async Task<IActionResult> Prescribe(Guid id, [FromBody] Prescription prescription)
        {
            var authResult = await _auth.AuthorizeAsync(User, id, "CanPrescribe");
            if (!authResult.Succeeded) return Forbid();

            var patient = await _db.PatientRecords.FindAsync(id);
            if (patient == null) return NotFound();

            var userId = Guid.Parse(User.FindFirst("id")!.Value);
            prescription.PatientRecordId = id;
            prescription.PrescribedByUserId = userId;

            _db.Prescriptions.Add(prescription);
            await _db.SaveChangesAsync();
            return Ok(prescription);
        }

        // View test results (doctor assigned or admin or patient self)
        [HttpGet("{id:guid}/tests")]
        [Authorize(Policy = "CanViewPatient")]
        public async Task<IActionResult> GetTests(Guid id)
        {
            var authResult = await _auth.AuthorizeAsync(User, id, "CanViewPatient");
            if (!authResult.Succeeded) return Forbid();

            var results = await _db.TestResults.Where(t => t.PatientRecordId == id).ToListAsync();
            return Ok(results);
        }
    }


}
