using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace MediCare.Services
{
    public interface IPdfService
    {
        Task<byte[]> GenerateAppointmentPdfAsync(string title, string content, string patientName, string doctorName, DateTime appointmentDate);
        Task<byte[]> GenerateNurseAssignmentPdfAsync(string nurseName, string patientName, string doctorName, DateTime appointmentDate, string consultationType);
    }

    public class PdfService : IPdfService
    {
        public async Task<byte[]> GenerateAppointmentPdfAsync(string title, string content, string patientName, string doctorName, DateTime appointmentDate)
        {
            return await Task.Run(() =>
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        page.Header()
                            .AlignCenter()
                            .Text(title)
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(20);

                                // Appointment Details Section
                                column.Item().Background(Colors.Grey.Lighten3)
                                    .Padding(15)
                                    .Column(detailsColumn =>
                                    {
                                        detailsColumn.Spacing(10);
                                        
                                        detailsColumn.Item().Text($"Patient: {patientName}").SemiBold();
                                        detailsColumn.Item().Text($"Doctor: Dr. {doctorName}").SemiBold();
                                        detailsColumn.Item().Text($"Date: {appointmentDate:MMMM dd, yyyy}").SemiBold();
                                        detailsColumn.Item().Text($"Time: {appointmentDate:hh:mm tt}").SemiBold();
                                        detailsColumn.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}").SemiBold();
                                    });

                                // Content Section
                                column.Item().Text(content);
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                });

                return document.GeneratePdf();
            });
        }

        public async Task<byte[]> GenerateNurseAssignmentPdfAsync(string nurseName, string patientName, string doctorName, DateTime appointmentDate, string consultationType)
        {
            return await Task.Run(() =>
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        page.Header()
                            .AlignCenter()
                            .Text("Nurse Assignment Notification")
                            .SemiBold().FontSize(20).FontColor(Colors.Purple.Medium);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(20);

                                // Assignment Details
                                column.Item().Background(Colors.Grey.Lighten3)
                                    .Padding(15)
                                    .Column(detailsColumn =>
                                    {
                                        detailsColumn.Spacing(10);
                                        
                                        detailsColumn.Item().Text($"Assigned Nurse: {nurseName}").SemiBold();
                                        detailsColumn.Item().Text($"Patient: {patientName}").SemiBold();
                                        detailsColumn.Item().Text($"Doctor: Dr. {doctorName}").SemiBold();
                                        detailsColumn.Item().Text($"Appointment Date: {appointmentDate:MMMM dd, yyyy}").SemiBold();
                                        detailsColumn.Item().Text($"Time: {appointmentDate:hh:mm tt}").SemiBold();
                                        detailsColumn.Item().Text($"Consultation Type: {consultationType}").SemiBold();
                                        detailsColumn.Item().Text($"Assigned On: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}").SemiBold();
                                    });

                                // Instructions Section
                                column.Item().Column(instructionsColumn =>
                                {
                                    instructionsColumn.Spacing(10);
                                    
                                    instructionsColumn.Item().Text("Responsibilities:").SemiBold().FontSize(14);
                                    instructionsColumn.Item().Text("• Review patient records before consultation");
                                    instructionsColumn.Item().Text("• Prepare necessary medical equipment");
                                    instructionsColumn.Item().Text("• Assist doctor during the procedure");
                                    instructionsColumn.Item().Text("• Document patient vitals and observations");
                                    instructionsColumn.Item().Text("• Provide post-consultation care instructions");
                                });

                                // Notes Section
                                column.Item().Background(Colors.Yellow.Lighten5)
                                    .Padding(15)
                                    .Text("Please ensure you are familiar with the patient's medical history and any specific requirements for this consultation type.")
                                    .Italic();
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Medicare Healthcare System - ");
                                x.Span(DateTime.Now.Year.ToString());
                            });
                    });
                });

                return document.GeneratePdf();
            });
        }
    }
}