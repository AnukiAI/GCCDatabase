namespace GCCWebApp.Models
{
    public class DashboardViewModel
    {
        public string UserRole { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool HasError { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;

        // Certificate Statistics
        public Dictionary<string, int> Statistics { get; set; } = new();
        public int TotalActiveCertificates { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCertificationTypes { get; set; }

        // Certificate Collections
        public IEnumerable<Certificate> RecentCertificates { get; set; } = new List<Certificate>();
        public IEnumerable<Certificate> ExpiringCertificates { get; set; } = new List<Certificate>();

        // Dashboard breakdown data
        public IEnumerable<(string Name, int Count)> CertificationTypeBreakdown { get; set; } = new List<(string, int)>();
        public IEnumerable<(string Name, int Count)> TopCompaniesBreakdown { get; set; } = new List<(string, int)>();
        public IEnumerable<(string Name, int Count)> ProductTypeBreakdown { get; set; } = new List<(string, int)>();

        // Computed Properties
        public int TotalCertificates => Statistics.GetValueOrDefault("Total", 0);
        public int ActiveCertificates => Statistics.GetValueOrDefault("Active", 0);
        public int ExpiredCertificates => Statistics.GetValueOrDefault("Expired", 0);
        public int ExpiringSoonCertificates => Statistics.GetValueOrDefault("ExpiringSoon", 0);

        public bool IsAdmin => UserRole == "Admin";
        public bool IsAnalyst => UserRole == "Analyst";
        public bool IsInternal => IsAdmin || IsAnalyst;
        public bool IsExternal => UserRole == "External";
        public bool IsPublic => UserRole == "Public";
    }

    public class StatItem
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = "primary";
        public string Description { get; set; } = string.Empty;
    }

    public class CertificationTypeSummary
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public int ActiveCount { get; set; }
        public int ExpiredCount { get; set; }
        public double Percentage { get; set; }
    }
}