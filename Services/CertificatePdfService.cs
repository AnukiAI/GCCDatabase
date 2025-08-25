using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface ICertificatePdfService
    {
        Task<byte[]> GenerateCertificatePdfAsync(Certificate certificate);
    }

    public class CertificatePdfService : ICertificatePdfService
    {
        private readonly ILogger<CertificatePdfService> _logger;

        public CertificatePdfService(ILogger<CertificatePdfService> logger)
        {
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateCertificatePdfAsync(Certificate certificate)
        {
            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        page.Header()
                            .AlignCenter()
                            .Text("GREEN CIRCLE CERTIFIED")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Green.Darken3);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Item().AlignCenter().Text("CERTIFICATE OF COMPLIANCE")
                                    .FontSize(18).Bold().FontColor(Colors.Green.Darken2);

                                column.Item().PaddingTop(20).Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Certificate Number:").Bold();
                                        col.Item().PaddingBottom(10).Text(certificate.Number ?? "N/A");

                                        col.Item().Text("Company:").Bold();
                                        col.Item().PaddingBottom(10).Text(certificate.Company?.Name ?? "N/A");

                                        col.Item().Text("Product:").Bold();
                                        col.Item().PaddingBottom(10).Text(certificate.Product?.Name ?? "N/A");

                                        col.Item().Text("Certification Type:").Bold();
                                        col.Item().PaddingBottom(10).Text(certificate.CertificationType?.Name ?? "N/A");
                                    });

                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Created Date:").Bold();
                                        col.Item().PaddingBottom(10).Text(certificate.CreatedAt.ToString("MMMM dd, yyyy"));

                                        col.Item().Text("Effective Date:").Bold();
                                        col.Item().PaddingBottom(10).Text(certificate.EffectiveDate.ToString("MMMM dd, yyyy"));

                                        col.Item().Text("Expiration Date:").Bold();
                                        col.Item().PaddingBottom(10).Text(certificate.ExpirationDate.ToString("MMMM dd, yyyy"));

                                        col.Item().Text("Status:").Bold();
                                        col.Item().PaddingBottom(10).Text(certificate.Status ?? "N/A");
                                    });
                                });

                                column.Item().PaddingTop(20).Text("Product Information:").Bold();
                                column.Item().Text($"ASIN: {certificate.Asin ?? "N/A"}")
                                    .LineHeight(1.5f);

                                if (!string.IsNullOrEmpty(certificate.Location))
                                {
                                    column.Item().PaddingTop(15).Text("Location:").Bold();
                                    column.Item().Text(certificate.Location);
                                }

                                if (!string.IsNullOrEmpty(certificate.Country))
                                {
                                    column.Item().PaddingTop(10).Text("Country:").Bold();
                                    column.Item().Text(certificate.Country);
                                }

                                column.Item().PaddingTop(30).AlignCenter()
                                    .Text("This certificate confirms that the above product meets Green Circle Certified standards.")
                                    .Italic().FontColor(Colors.Grey.Darken2);
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.CurrentPageNumber().FontColor(Colors.Grey.Medium);
                                text.Span(" / ").FontColor(Colors.Grey.Medium);
                                text.TotalPages().FontColor(Colors.Grey.Medium);
                            });
                    });
                });

                return await Task.FromResult(document.GeneratePdf());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for certificate {CertificateNumber}", certificate.Number);
                throw;
            }
        }
    }
}