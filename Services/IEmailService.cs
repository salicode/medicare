using Microsoft.Extensions.Logging;

namespace MediCare
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(string email, string confirmationLink);
        Task SendPasswordResetEmailAsync(string email, string resetLink);

        Task SendAppointmentConfirmationAsync(string email, string appointmentDetails);

        // Task SendAppointmentConfirmationAsync(
        //     string email, 
        //     string appointmentDetails, 
        //     byte[]? pdfAttachment = null, 
        //     string? attachmentName = null);
    }
    }

