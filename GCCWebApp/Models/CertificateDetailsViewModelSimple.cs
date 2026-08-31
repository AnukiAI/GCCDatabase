namespace GCCWebApp.Models
{
    public class CertificateDetailsViewModelSimple
    {
        public Certificate Certificate { get; set; } = null!;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public bool ShowInternalData { get; set; } = false;
        public List<GCCWebApp.Services.CustomFieldValueDisplay>? CustomFieldValues { get; set; }

        /// <summary>
        /// DEF-369: true when this certificate's NUMBER is used by more than one certificate
        /// (CR-038 — 137 numbers, 312 certificates). Only then does the shareable "Database Listing
        /// URL" need the ?cid= disambiguator; for a unique number the short URL is kept, which is
        /// the whole point of disambiguating rather than re-keying every link on the id.
        /// </summary>
        public bool SharesCertificateNumber { get; set; } = false;
    }
}
