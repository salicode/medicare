using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediCare.MediCare;
using MediCare.Models.Entities;
using MediCare.Models.DTOs.Consultations;

using MediCare.Models.DTOs.Auth;
using MediCare.Helpers;
using System.Linq;

namespace MediCare.Models.Entities
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConsultationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly IAuthorizationService _auth;
        private readonly ILogger<ConsultationsController> _logger;

        public ConsultationsController(
            ApplicationDbContext db,
            IEmailService emailService,
            IAuthorizationService auth,
            ILogger<ConsultationsController> logger)
        {
            _db = db;
            _emailService = emailService;
            _auth = auth;
            _logger = logger;
        }

        // [HttpPost("book")]
        // [Authorize(Roles = RoleConstants.Patient)]
        // public async Task<IActionResult> BookConsultation([FromBody] BookConsultationRequest request)
        // {
        //     try
        //     {
        //         var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);

        //         // Verify patient record exists and belongs to current user
        //         var patientRecord = await _db.PatientRecords
        //             .FirstOrDefaultAsync(p => p.Id == request.PatientRecordId);

        //         if (patientRecord == null)
        //             return NotFound("Patient record not found");

        //         // Check if patient record belongs to current user
        //         var user = await _db.Users.FindAsync(currentUserId);
        //         if (user?.PatientProfileId != request.PatientRecordId)
        //             return Forbid("You can only book consultations for your own patient record");

        //         // Verify doctor exists and is active
        //         var doctor = await _db.Doctors
        //             .Include(d => d.User)
        //             .Include(d => d.Specialization)
        //             .FirstOrDefaultAsync(d => d.Id == request.DoctorId && d.IsActive);

        //         if (doctor == null)
        //             return NotFound("Doctor not found");

        //         // Check if slot is available
        //         var isSlotAvailable = await IsTimeSlotAvailable(request.DoctorId, request.ScheduledAt);
        //         if (!isSlotAvailable)
        //             return BadRequest("Selected time slot is not available");

        //         var consultation = new Consultation
        //         {
        //             PatientRecordId = request.PatientRecordId,
        //             DoctorId = request.DoctorId,
        //             ScheduledAt = request.ScheduledAt,
        //             Duration = TimeSpan.FromMinutes(30),
        //             ConsultationType = (ConsultationType)request.ConsultationType,
        //             Status = AppointmentStatus.Pending,
        //             Symptoms = request.Symptoms,
        //             Fee = doctor.ConsultationFee
        //         };

        //         _db.Consultations.Add(consultation);
        //         await _db.SaveChangesAsync();

        //         // Send confirmation emails to both patient and doctor
        //         await SendAppointmentConfirmationEmails(consultation, doctor, user);

        //         _logger.LogInformation("Consultation booked: {ConsultationId} by user {UserId}", consultation.Id, currentUserId);

        //         return Ok(new
        //         {
        //             ConsultationId = consultation.Id,
        //             Message = "Consultation booked successfully"
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error booking consultation");
        //         return StatusCode(500, "An error occurred while booking consultation");
        //     }
        // }

        [HttpPost("book")]
        [Authorize(Roles = RoleConstants.Patient)]
        public async Task<IActionResult> BookConsultation([FromBody] BookConsultationRequest request)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);

                // FIX: Convert scheduledAt to UTC
                var scheduledAtUtc = request.ScheduledAt;
                if (scheduledAtUtc.Kind == DateTimeKind.Unspecified)
                {
                    scheduledAtUtc = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);
                }
                else if (scheduledAtUtc.Kind == DateTimeKind.Local)
                {
                    scheduledAtUtc = scheduledAtUtc.ToUniversalTime();
                }

                // Verify patient record exists and belongs to current user
                var patientRecordExists = await _db.PatientRecords
                    .AnyAsync(p => p.Id == request.PatientRecordId);

                if (!patientRecordExists)
                    return NotFound("Patient record not found");

                // Check if patient record belongs to current user
                var user = await _db.Users.FindAsync(currentUserId);
                if (user?.PatientProfileId != request.PatientRecordId)
                    return Forbid("You can only book consultations for your own patient record");

                // Verify doctor exists and is active
                var doctor = await _db.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Specialization)
                    .FirstOrDefaultAsync(d => d.Id == request.DoctorId && d.IsActive);

                if (doctor == null)
                    return NotFound("Doctor not found");

                // Check if slot is available - pass the UTC time
                var isSlotAvailable = await IsTimeSlotAvailable(request.DoctorId, scheduledAtUtc);
                if (!isSlotAvailable)
                    return BadRequest("Selected time slot is not available");

                var consultation = new Consultation
                {
                    PatientRecordId = request.PatientRecordId,
                    DoctorId = request.DoctorId,
                    ScheduledAt = scheduledAtUtc, // Use UTC time
                    Duration = TimeSpan.FromMinutes(30),
                    ConsultationType = (ConsultationType)request.ConsultationType,
                    Status = AppointmentStatus.Pending,
                    Symptoms = request.Symptoms,
                    Fee = doctor.ConsultationFee
                };

                _db.Consultations.Add(consultation);
                await _db.SaveChangesAsync();

                // Send confirmation emails to both patient and doctor
                await SendAppointmentConfirmationEmails(consultation, doctor, user);

                _logger.LogInformation("Consultation booked: {ConsultationId} by user {UserId}", consultation.Id, currentUserId);

                return Ok(new
                {
                    ConsultationId = consultation.Id,
                    Message = "Consultation booked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking consultation");
                return StatusCode(500, "An error occurred while booking consultation");
            }
        }

        [HttpPost("{id:guid}/assign-nurse")]
        [Authorize(Roles = $"{RoleConstants.Doctor},{RoleConstants.SuperAdmin}")]
        public async Task<IActionResult> AssignNurse(Guid id, [FromBody] AssignNurseRequest request)
        {
            try
            {
                var consultation = await _db.Consultations
                    .Include(c => c.Doctor)
                    .Include(c => c.PatientRecord)
                    .Include(c => c.Doctor.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null) return NotFound("Consultation not found");

                // Verify the current user is the assigned doctor or admin
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);
                if (consultation.Doctor.UserId != currentUserId && !User.IsInRole(RoleConstants.SuperAdmin))
                    return Forbid();

                // Verify nurse exists and has nurse role
                var nurse = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == request.NurseId && u.UserRoles.Any(ur => ur.Role.Name == RoleConstants.Nurse));

                if (nurse == null) return NotFound("Nurse not found");

                consultation.NurseId = request.NurseId;
                consultation.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                // Create assignment record for nurse to access patient records
                var assignment = new UserPatientAssignment
                {
                    UserId = request.NurseId,
                    PatientRecordId = consultation.PatientRecordId
                };

                _db.UserPatientAssignments.Add(assignment);
                await _db.SaveChangesAsync();

                // Send notification email to nurse
                await SendNurseAssignmentEmail(consultation, nurse);

                _logger.LogInformation("Nurse {NurseId} assigned to consultation {ConsultationId}", request.NurseId, id);

                return Ok(new { Message = "Nurse assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning nurse to consultation {ConsultationId}", id);
                return StatusCode(500, "An error occurred while assigning nurse");
            }
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = $"{RoleConstants.Doctor},{RoleConstants.SuperAdmin}")]
        public async Task<IActionResult> UpdateConsultationStatus(Guid id, [FromBody] UpdateConsultationStatusRequest request)
        {
            try
            {
                var consultation = await _db.Consultations
                    .Include(c => c.Doctor)
                    .Include(c => c.PatientRecord)
                    .Include(c => c.Doctor.User)
                    .Include(c => c.Doctor.Specialization)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null) return NotFound("Consultation not found");

                // Verify the current user is the assigned doctor or admin
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);
                if (consultation.Doctor.UserId != currentUserId && !User.IsInRole(RoleConstants.SuperAdmin))
                    return Forbid();

                var oldStatus = consultation.Status;
                consultation.Status = (AppointmentStatus)request.Status;
                consultation.UpdatedAt = DateTime.UtcNow;

                if ((AppointmentStatus)request.Status == AppointmentStatus.Completed)
                {
                    consultation.CompletedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                // Send status update email to patient
                if ((int)oldStatus != (int)request.Status)
                {
                    await SendStatusUpdateEmail(consultation, oldStatus, request.Status);
                }

                _logger.LogInformation("Consultation {ConsultationId} status updated from {OldStatus} to {NewStatus}",
                    id, oldStatus, request.Status);

                return Ok(new { Message = "Consultation status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consultation status {ConsultationId}", id);
                return StatusCode(500, "An error occurred while updating consultation status");
            }
        }

        private async Task SendStatusUpdateEmail(Consultation consultation, AppointmentStatus oldStatus, MediCare.AppointmentStatus status)
        {
            throw new NotImplementedException();
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = $"{RoleConstants.Doctor},{RoleConstants.SuperAdmin}")]
        public async Task<IActionResult> UpdateConsultation(Guid id, [FromBody] UpdateConsultationRequest request)
        {
            try
            {
                var consultation = await _db.Consultations
                    .Include(c => c.Doctor)
                    .Include(c => c.PatientRecord)
                    .Include(c => c.Doctor.User)
                    .Include(c => c.Doctor.Specialization)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null) return NotFound("Consultation not found");

                // Verify the current user is the assigned doctor or admin
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);
                if (consultation.Doctor.UserId != currentUserId && !User.IsInRole(RoleConstants.SuperAdmin))
                    return Forbid();

                var hadUpdates = false;
                var oldStatus = consultation.Status;

                // Track what was updated for the notification
                var updates = new List<string>();

                if (!string.IsNullOrEmpty(request.Diagnosis) && request.Diagnosis != consultation.Diagnosis)
                {
                    consultation.Diagnosis = request.Diagnosis;
                    updates.Add("diagnosis");
                    hadUpdates = true;
                }

                if (!string.IsNullOrEmpty(request.TreatmentPlan) && request.TreatmentPlan != consultation.TreatmentPlan)
                {
                    consultation.TreatmentPlan = request.TreatmentPlan;
                    updates.Add("treatment plan");
                    hadUpdates = true;
                }

                if (!string.IsNullOrEmpty(request.Notes) && request.Notes != consultation.Notes)
                {
                    consultation.Notes = request.Notes;
                    updates.Add("notes");
                    hadUpdates = true;
                }

                if ((int)request.Status != (int)consultation.Status)
                {
                    consultation.Status = (AppointmentStatus)request.Status;
                    updates.Add($"status to {request.Status}");
                    hadUpdates = true;

                    if ((AppointmentStatus)request.Status == AppointmentStatus.Completed)
                    {
                        consultation.CompletedAt = DateTime.UtcNow;
                    }
                }

                consultation.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                // Send update notification if there were significant changes
                if (hadUpdates && updates.Any())
                {
                    await SendConsultationUpdateEmail(consultation, updates);
                }

                _logger.LogInformation("Consultation {ConsultationId} updated: {Updates}", id, string.Join(", ", updates));

                return Ok(new { Message = "Consultation updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consultation {ConsultationId}", id);
                return StatusCode(500, "An error occurred while updating consultation");
            }
        }

        [HttpPost("{id:guid}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelConsultation(Guid id)
        {
            try
            {
                var consultation = await _db.Consultations
                    .Include(c => c.Doctor)
                    .Include(c => c.PatientRecord)
                    .Include(c => c.Doctor.User)
                    .Include(c => c.Doctor.Specialization)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null) return NotFound("Consultation not found");

                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

                // Check authorization: Patient can cancel their own, Doctor can cancel their appointments
                if (userRoles.Contains(RoleConstants.Patient))
                {
                    var user = await _db.Users.FindAsync(currentUserId);
                    if (user?.PatientProfileId != consultation.PatientRecordId)
                        return Forbid("You can only cancel your own consultations");
                }
                else if (userRoles.Contains(RoleConstants.Doctor))
                {
                    if (consultation.Doctor.UserId != currentUserId)
                        return Forbid("You can only cancel your own consultations");
                }
                else if (!userRoles.Contains(RoleConstants.SuperAdmin))
                {
                    return Forbid();
                }

                var oldStatus = consultation.Status;
                consultation.Status = AppointmentStatus.Cancelled;
                consultation.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                // Send cancellation emails
                await SendCancellationEmails(consultation, currentUserId);

                _logger.LogInformation("Consultation {ConsultationId} cancelled by user {UserId}", id, currentUserId);

                return Ok(new { Message = "Consultation cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling consultation {ConsultationId}", id);
                return StatusCode(500, "An error occurred while cancelling consultation");
            }
        }

        [HttpGet("my-consultations")]
        public async Task<IActionResult> GetMyConsultations()
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("id")!.Value);
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

                IQueryable<Consultation> query = _db.Consultations
                    .Include(c => c.Doctor)
                    .ThenInclude(d => d.Specialization)
                    .Include(c => c.PatientRecord)
                    .Include(c => c.Nurse);

                if (userRoles.Contains(RoleConstants.Patient))
                {
                    // Patient sees their own consultations
                    var patientRecordId = await _db.Users
                        .Where(u => u.Id == currentUserId)
                        .Select(u => u.PatientProfileId)
                        .FirstOrDefaultAsync();

                    if (patientRecordId == null) return Ok(new List<Consultation>());

                    query = query.Where(c => c.PatientRecordId == patientRecordId.Value);
                }
                else if (userRoles.Contains(RoleConstants.Doctor))
                {
                    // Doctor sees consultations assigned to them
                    var doctor = await _db.Doctors
                        .FirstOrDefaultAsync(d => d.UserId == currentUserId);

                    if (doctor == null) return Ok(new List<Consultation>());

                    query = query.Where(c => c.DoctorId == doctor.Id);
                }
                else if (userRoles.Contains(RoleConstants.Nurse))
                {
                    // Nurse sees consultations where they are assigned
                    query = query.Where(c => c.NurseId == currentUserId);
                }

                var consultations = await query
                    .OrderByDescending(c => c.ScheduledAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.ScheduledAt,
                        c.Duration,
                        c.ConsultationType,
                        c.Status,
                        c.Fee,
                        DoctorName = c.Doctor.FullName,
                        Specialization = c.Doctor.Specialization.Name,
                        PatientName = c.PatientRecord.FullName,
                        NurseName = c.Nurse != null ? c.Nurse.Username : null
                    })
                    .ToListAsync();

                return Ok(consultations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consultations");
                return StatusCode(500, "An error occurred while retrieving consultations");
            }
        }

        private async Task SendAppointmentConfirmationEmails(Consultation consultation, Doctor doctor, User patientUser)
        {
            try
            {
                // Email to Patient
                var patientEmailBody = $@"
            <h3 style='color: #007bff;'>Appointment Confirmed!</h3>
            <p>Dear <strong>{patientUser.Username}</strong>,</p>
            
            <p>Your appointment has been successfully booked with <strong>Dr. {doctor.FullName}</strong> (<em>{doctor.Specialization.Name}</em>).</p>
            
            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                <h4 style='margin-top: 0; color: #495057;'>Appointment Details:</h4>
                <table style='width: 100%;'>
                    <tr><td style='padding: 5px;'><strong>Date:</strong></td><td style='padding: 5px;'>{consultation.ScheduledAt:MMMM dd, yyyy}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Time:</strong></td><td style='padding: 5px;'>{consultation.ScheduledAt:hh:mm tt}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Duration:</strong></td><td style='padding: 5px;'>{consultation.Duration.TotalMinutes} minutes</td></tr>
                    <tr><td style='padding: 5px;'><strong>Type:</strong></td><td style='padding: 5px;'>{consultation.ConsultationType}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Fee:</strong></td><td style='padding: 5px;'>${consultation.Fee}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Symptoms:</strong></td><td style='padding: 5px;'>{consultation.Symptoms ?? "Not specified"}</td></tr>
                </table>
            </div>
            
            <p style='color: #dc3545;'><strong>Please arrive 10-15 minutes before your scheduled time.</strong></p>
            
            <p>We look forward to helping you with your healthcare needs.</p>";

                await _emailService.SendAppointmentConfirmationAsync(patientUser.Email, patientEmailBody);

                // Email to Doctor
                var doctorEmailBody = $@"
            <h3 style='color: #28a745;'>New Appointment Booked</h3>
            <p>Dear <strong>Dr. {doctor.FullName}</strong>,</p>
            
            <p>A new appointment has been booked with you.</p>
            
            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                <h4 style='margin-top: 0; color: #495057;'>Appointment Details:</h4>
                <table style='width: 100%;'>
                    <tr><td style='padding: 5px;'><strong>Patient:</strong></td><td style='padding: 5px;'>{patientUser.Username}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Email:</strong></td><td style='padding: 5px;'>{patientUser.Email}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Date:</strong></td><td style='padding: 5px;'>{consultation.ScheduledAt:MMMM dd, yyyy}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Time:</strong></td><td style='padding: 5px;'>{consultation.ScheduledAt:hh:mm tt}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Duration:</strong></td><td style='padding: 5px;'>{consultation.Duration.TotalMinutes} minutes</td></tr>
                    <tr><td style='padding: 5px;'><strong>Type:</strong></td><td style='padding: 5px;'>{consultation.ConsultationType}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Symptoms:</strong></td><td style='padding: 5px;'>{consultation.Symptoms ?? "Not specified"}</td></tr>
                </table>
            </div>
            
            <p>Please review the patient's information before the appointment.</p>";

                await _emailService.SendAppointmentConfirmationAsync(doctor.User.Email, doctorEmailBody);

                _logger.LogInformation("Appointment confirmation emails sent for consultation {ConsultationId}", consultation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending appointment confirmation emails for consultation {ConsultationId}", consultation.Id);
                // Don't throw - email failure shouldn't break the booking process
            }
        }

        private async Task SendNurseAssignmentEmail(Consultation consultation, User nurse)
        {
            try
            {
                var nurseEmailBody = $@"
            <h3 style='color: #6f42c1;'>Nurse Assignment Notification</h3>
            <p>Dear <strong>{nurse.Username}</strong>,</p>
            
            <p>You have been assigned to assist with a patient consultation.</p>
            
            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                <h4 style='margin-top: 0; color: #495057;'>Consultation Details:</h4>
                <table style='width: 100%;'>
                    <tr><td style='padding: 5px;'><strong>Patient:</strong></td><td style='padding: 5px;'>{consultation.PatientRecord.FullName}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Doctor:</strong></td><td style='padding: 5px;'>Dr. {consultation.Doctor.FullName}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Date:</strong></td><td style='padding: 5px;'>{consultation.ScheduledAt:MMMM dd, yyyy}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Time:</strong></td><td style='padding: 5px;'>{consultation.ScheduledAt:hh:mm tt}</td></tr>
                    <tr><td style='padding: 5px;'><strong>Type:</strong></td><td style='padding: 5px;'>{consultation.ConsultationType}</td></tr>
                </table>
            </div>
            
            <p>Please review the patient's records and be prepared to assist during the consultation.</p>";

                await _emailService.SendAppointmentConfirmationAsync(nurse.Email, nurseEmailBody);

                _logger.LogInformation("Nurse assignment email sent to {NurseEmail} for consultation {ConsultationId}", nurse.Email, consultation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending nurse assignment email for consultation {ConsultationId}", consultation.Id);
            }
        }

        // Update other email methods similarly with HTML formatting...

        private async Task SendStatusUpdateEmail(Consultation consultation, AppointmentStatus oldStatus, AppointmentStatus newStatus)
        {
            try
            {
                var patientUser = await _db.Users
                    .FirstOrDefaultAsync(u => u.PatientProfileId == consultation.PatientRecordId);

                if (patientUser == null) return;

                var emailBody = $@"
        Dear {patientUser.Username},

        Your appointment status has been updated.

        Appointment Details:
        - Doctor: Dr. {consultation.Doctor.FullName} ({consultation.Doctor.Specialization.Name})
        - Date: {consultation.ScheduledAt:MMMM dd, yyyy}
        - Time: {consultation.ScheduledAt:hh:mm tt}

        Status changed from {oldStatus} to {newStatus}.

        ";

                if (newStatus == AppointmentStatus.Completed && !string.IsNullOrEmpty(consultation.TreatmentPlan))
                {
                    emailBody += $@"
            Treatment Plan:
            {consultation.TreatmentPlan}

            Please follow the above instructions and contact us if you have any questions.
            ";
                }

                emailBody += "\nThank you for choosing MediCare.";

                await _emailService.SendAppointmentConfirmationAsync(patientUser.Email, emailBody);

                _logger.LogInformation("Status update email sent for consultation {ConsultationId}", consultation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending status update email for consultation {ConsultationId}", consultation.Id);
            }
        }

        private async Task SendConsultationUpdateEmail(Consultation consultation, List<string> updates)
        {
            try
            {
                var patientUser = await _db.Users
                    .FirstOrDefaultAsync(u => u.PatientProfileId == consultation.PatientRecordId);

                if (patientUser == null) return;

                var emailBody = $@"
                Dear {patientUser.Username},

                Your consultation details have been updated.

                Appointment with Dr. {consultation.Doctor.FullName} on {consultation.ScheduledAt:MMMM dd, yyyy}

                The following information was updated:
                - {string.Join("\n- ", updates)}

                ";

                if (!string.IsNullOrEmpty(consultation.TreatmentPlan))
                {
                    emailBody += $@"
            Current Treatment Plan:
            {consultation.TreatmentPlan}
            ";
                }

                emailBody += "\nPlease review the updated information.";

                await _emailService.SendAppointmentConfirmationAsync(patientUser.Email, emailBody);

                _logger.LogInformation("Consultation update email sent for consultation {ConsultationId}", consultation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending consultation update email for consultation {ConsultationId}", consultation.Id);
            }
        }

        private async Task SendCancellationEmails(Consultation consultation, Guid cancelledByUserId)
        {
            try
            {
                var patientUser = await _db.Users
                    .FirstOrDefaultAsync(u => u.PatientProfileId == consultation.PatientRecordId);

                if (patientUser == null) return;

                var cancelledByUser = await _db.Users.FindAsync(cancelledByUserId);
                var cancelledByName = cancelledByUser?.Username ?? "System";

                // Email to Patient
                var patientEmailBody = $@"
            Dear {patientUser.Username},

            Your appointment has been cancelled.

            Appointment Details:
            - Doctor: Dr. {consultation.Doctor.FullName} ({consultation.Doctor.Specialization.Name})
            - Date: {consultation.ScheduledAt:MMMM dd, yyyy}
            - Time: {consultation.ScheduledAt:hh:mm tt}

            Cancelled by: {cancelledByName}

            If this was unexpected or you need to reschedule, please contact us.

            Thank you,
            MediCare Team
            ";

                await _emailService.SendAppointmentConfirmationAsync(patientUser.Email, patientEmailBody);

                // Email to Doctor
                var doctorEmailBody = $@"
                Dr. {consultation.Doctor.FullName},

                Your appointment has been cancelled.

                Appointment Details:
                - Patient: {patientUser.Username}
                - Date: {consultation.ScheduledAt:MMMM dd, yyyy}
                - Time: {consultation.ScheduledAt:hh:mm tt}

                Cancelled by: {cancelledByName}

                This time slot is now available for other appointments.

                MediCare System
                ";

                await _emailService.SendAppointmentConfirmationAsync(consultation.Doctor.User.Email, doctorEmailBody);

                _logger.LogInformation("Cancellation emails sent for consultation {ConsultationId}", consultation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending cancellation emails for consultation {ConsultationId}", consultation.Id);
            }
        }



        private async Task<bool> IsTimeSlotAvailable(Guid doctorId, DateTime scheduledAt)
        {
            // Ensure scheduledAt is UTC
            var scheduledAtUtc = scheduledAt.Kind == DateTimeKind.Utc ? scheduledAt : scheduledAt.ToUniversalTime();

            var consultationDuration = TimeSpan.FromMinutes(30);
            var consultationEndTime = scheduledAtUtc.Add(consultationDuration);

            // Check against doctor's availability
            var dayOfWeek = scheduledAtUtc.DayOfWeek;
            var timeOfDay = scheduledAtUtc.TimeOfDay;

            var isAvailable = await _db.DoctorAvailabilities
                .AnyAsync(a => a.DoctorId == doctorId &&
                    ((a.IsRecurring && a.DayOfWeek == dayOfWeek) ||
                     (!a.IsRecurring && a.SpecificDate == scheduledAtUtc.Date)) &&
                    a.StartTime <= timeOfDay &&
                    a.EndTime >= consultationEndTime.TimeOfDay);

            if (!isAvailable) return false;

            // Check for overlapping appointments
            var existingAppointments = await _db.Consultations
                .Where(c => c.DoctorId == doctorId &&
                           c.Status != AppointmentStatus.Cancelled &&
                           c.ScheduledAt.Date == scheduledAtUtc.Date)
                .Select(c => new { c.ScheduledAt, c.Duration })
                .ToListAsync();

            // Calculate overlaps in memory
            var overlappingCount = existingAppointments.Count(c =>
            {
                var existingEnd = c.ScheduledAt.Add(c.Duration);
                return scheduledAtUtc < existingEnd && consultationEndTime > c.ScheduledAt;
            });

            // Check against max appointments per slot
            var availability = await _db.DoctorAvailabilities
                .FirstOrDefaultAsync(a => a.DoctorId == doctorId &&
                    ((a.IsRecurring && a.DayOfWeek == dayOfWeek) ||
                     (!a.IsRecurring && a.SpecificDate == scheduledAtUtc.Date)) &&
                    a.StartTime <= timeOfDay &&
                    a.EndTime >= consultationEndTime.TimeOfDay);

            return overlappingCount < (availability?.MaxAppointmentsPerSlot ?? 1);
        }



        private async Task<List<AvailableSlotResponse>> CalculateAvailableSlots(Guid doctorId, DateTime startDate, DateTime endDate)
        {
            var slots = new List<AvailableSlotResponse>();
            var currentTime = DateTime.Now;

            var availabilities = await _db.DoctorAvailabilities
                .Where(a => a.DoctorId == doctorId)
                .ToListAsync();

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

                    // Generate 30-minute slots within availability window
                    for (var slotTime = slotStart; slotTime < slotEnd; slotTime = slotTime.AddMinutes(30))
                    {
                        var slotEndTime = slotTime.AddMinutes(30);

                        // Only show future slots
                        if (slotTime < currentTime) continue;

                        var bookedCount = existingAppointments
                            .Count(a => a.ScheduledAt >= slotTime && a.ScheduledAt < slotEndTime);

                        var isAvailable = bookedCount < availability.MaxAppointmentsPerSlot;

                        slots.Add(new AvailableSlotResponse(
                            slotTime,
                            slotEndTime,
                            isAvailable,
                            bookedCount,
                            availability.MaxAppointmentsPerSlot
                        ));
                    }
                }
            }

            return slots.OrderBy(s => s.StartTime).ToList();
        }

    }
}


