namespace GCCWebApp.Models
{
    public class CertificateDetailsViewModelSimple
    {
        public Certificate Certificate { get; set; } = null!;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public bool ShowInternalData { get; set; } = false;
        public List<GCCWebApp.Services.CustomFieldValueDisplay>? CustomFieldValues { get; set; }
    }
}
