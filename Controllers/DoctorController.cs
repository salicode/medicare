using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MediCare.MediCare;

namespace MediCare.Models.Entities
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DoctorsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DoctorsController> _logger;

        public DoctorsController(ApplicationDbContext db, ILogger<DoctorsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetDoctors([FromQuery] Guid? specializationId = null)
        {
            try
            {
                var query = _db.Doctors
                    .Include(d => d.Specialization)
                    .Include(d => d.User)
                    .Where(d => d.IsActive);

                if (specializationId.HasValue)
                {
                    query = query.Where(d => d.SpecializationId == specializationId.Value);
                }

                var doctors = await query
                    .Select(d => new
                    {
                        d.Id,
                        d.FullName,
                        d.YearsOfExperience,
                        d.ConsultationFee,
                        d.Bio,
                        d.PhoneNumber,
                        Specialization = d.Specialization.Name,
                        IsAvailable = d.Availabilities.Any(a => a.IsRecurring || a.SpecificDate >= DateTime.Today)
                    })
                    .ToListAsync();

                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctors");
                return StatusCode(500, "An error occurred while retrieving doctors");
            }
        }

        [HttpGet("my-profile")]
        [Authorize(Roles = RoleConstants.Doctor)]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);

                var doctor = await _db.Doctors
                    .Include(d => d.Specialization)
                    .Include(d => d.Availabilities)
                    .FirstOrDefaultAsync(d => d.UserId == currentUserId);

                if (doctor == null)
                    return NotFound("Doctor profile not found");

                return Ok(new
                {
                    doctor.Id,
                    doctor.FullName,
                    doctor.YearsOfExperience,
                    doctor.ConsultationFee,
                    doctor.Bio,
                    doctor.PhoneNumber,
                    Specialization = doctor.Specialization.Name,
                    Availabilities = doctor.Availabilities.Select(a => new
                    {
                        a.DayOfWeek,
                        a.StartTime,
                        a.EndTime,
                        a.IsRecurring,
                        a.SpecificDate,
                        a.MaxAppointmentsPerSlot
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor profile");
                return StatusCode(500, "An error occurred while retrieving profile");
            }
        }

        [HttpPut("my-profile")]
        [Authorize(Roles = RoleConstants.Doctor)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateDoctorProfileRequest request)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);

                var doctor = await _db.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == currentUserId);

                if (doctor == null)
                    return NotFound("Doctor profile not found");

                // Verify specialization exists if provided
                if (request.SpecializationId.HasValue)
                {
                    var specialization = await _db.Specializations.FindAsync(request.SpecializationId.Value);
                    if (specialization == null)
                        return BadRequest("Invalid specialization");
                    doctor.SpecializationId = request.SpecializationId.Value;
                }

                doctor.FullName = request.FullName ?? doctor.FullName;
                doctor.PhoneNumber = request.PhoneNumber ?? doctor.PhoneNumber;
                doctor.Bio = request.Bio ?? doctor.Bio;
                doctor.YearsOfExperience = request.YearsOfExperience ?? doctor.YearsOfExperience;
                doctor.ConsultationFee = request.ConsultationFee ?? doctor.ConsultationFee;

                await _db.SaveChangesAsync();

                return Ok(new { Message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor profile");
                return StatusCode(500, "An error occurred while updating profile");
            }
        }

        [HttpGet("my-availability")]
        [Authorize(Roles = RoleConstants.Doctor)]
        public async Task<IActionResult> GetMyAvailability()
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);

                var doctor = await _db.Doctors
                    .Include(d => d.Availabilities)
                    .FirstOrDefaultAsync(d => d.UserId == currentUserId);

                if (doctor == null)
                    return NotFound("Doctor profile not found");

                var availabilities = doctor.Availabilities
                    .Select(a => new
                    {
                        a.Id,
                        a.DayOfWeek,
                        a.StartTime,
                        a.EndTime,
                        a.IsRecurring,
                        a.SpecificDate,
                        a.MaxAppointmentsPerSlot
                    })
                    .ToList();

                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor availability");
                return StatusCode(500, "An error occurred while retrieving availability");
            }
        }

        [HttpPost("my-availability")]
        [Authorize(Roles = RoleConstants.Doctor)]
        public async Task<IActionResult> AddMyAvailability([FromBody] DoctorAvailabilityRequest request)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);

                var doctor = await _db.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == currentUserId);

                if (doctor == null)
                    return NotFound("Doctor profile not found");

                var availability = new DoctorAvailability
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = request.DayOfWeek,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    IsRecurring = request.IsRecurring,
                    SpecificDate = request.SpecificDate,
                    MaxAppointmentsPerSlot = request.MaxAppointmentsPerSlot
                };

                _db.DoctorAvailabilities.Add(availability);
                await _db.SaveChangesAsync();

                return Ok(availability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding availability");
                return StatusCode(500, "An error occurred while adding availability");
            }
        }

        [HttpDelete("my-availability/{id:guid}")]
        [Authorize(Roles = RoleConstants.Doctor)]
        public async Task<IActionResult> DeleteMyAvailability(Guid id)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);

                var doctor = await _db.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == currentUserId);

                if (doctor == null)
                    return NotFound("Doctor profile not found");

                var availability = await _db.DoctorAvailabilities
                    .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);

                if (availability == null)
                    return NotFound("Availability not found");

                _db.DoctorAvailabilities.Remove(availability);
                await _db.SaveChangesAsync();

                return Ok(new { Message = "Availability deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting availability");
                return StatusCode(500, "An error occurred while deleting availability");
            }
        }

        [HttpGet("{id:guid}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDoctorAvailability(Guid id, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var doctor = await _db.Doctors
                    .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

                if (doctor == null)
                    return NotFound("Doctor not found");

                var availableSlots = await CalculateAvailableSlots(id, startDate, endDate);
                return Ok(availableSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving availability for doctor {DoctorId}", id);
                return StatusCode(500, "An error occurred while retrieving availability");
            }
        }

        private async Task<List<AvailableSlotResponse>> CalculateAvailableSlots(Guid doctorId, DateTime startDate, DateTime endDate)
        {
            var slots = new List<AvailableSlotResponse>();
            var currentTime = DateTime.Now;

            // Get doctor's availability
            var availabilities = await _db.DoctorAvailabilities
                .Where(a => a.DoctorId == doctorId)
                .ToListAsync();

            // Get existing appointments
            var existingAppointments = await _db.Consultations
                .Where(c => c.DoctorId == doctorId &&
                           c.ScheduledAt >= startDate &&
                           c.ScheduledAt <= endDate &&
                           c.Status != AppointmentStatus.Cancelled)
                .ToListAsync();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dayAvailabilities = availabilities.Where(a =>
                    (a.IsRecurring && a.DayOfWeek == date.DayOfWeek) ||
                    (!a.IsRecurring && a.SpecificDate == date.Date));

                foreach (var availability in dayAvailabilities)
                {
                    var slotStart = date.Add(availability.StartTime);
                    var slotEnd = date.Add(availability.EndTime);

                    // Only show future slots
                    if (slotStart < currentTime) continue;

                    var bookedCount = existingAppointments
                        .Count(a => a.ScheduledAt >= slotStart && a.ScheduledAt < slotEnd);

                    slots.Add(new AvailableSlotResponse(
                        slotStart,
                        slotEnd,
                        bookedCount < availability.MaxAppointmentsPerSlot,
                        bookedCount,
                        availability.MaxAppointmentsPerSlot
                    ));
                }
            }

            return slots;
        }
    }

    public record UpdateDoctorProfileRequest(
        string? FullName,
        Guid? SpecializationId,
        string? PhoneNumber,
        string? Bio,
        int? YearsOfExperience,
        decimal? ConsultationFee);
}