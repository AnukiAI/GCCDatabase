namespace GCCWebApp.Models
{
    /// <summary>
    /// Simple ViewModel for Certificate List page
    /// </summary>
    public class CertificateListViewModelSimple
    {
        public IEnumerable<Certificate> Certificates { get; set; } = new List<Certificate>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public string? SearchTerm { get; set; }
        public int? CompanyId { get; set; }
        public int? ProductId { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    /// <summary>
    /// Simple ViewModel for Certificate Details page
    /// </summary>
    public class CertificateDetailsViewModelSimple
    {
        public Certificate Certificate { get; set; } = null!;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public bool ShowInternalData { get; set; } = false;
    }
}