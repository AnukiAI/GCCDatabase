using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class CertificationType
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        // CR-187: persisted computed search-normalized form of Name (see Search.SearchNormalizer).
        public string? NormalizedName { get; private set; }

        [MaxLength(255)]
        public string? AmazonCode { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Hierarchy level for display (1 = parent, 2 = child/indented)
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// Parent certification type ID for hierarchical relationships (null for top-level types)
        /// </summary>
        public int? ParentId { get; set; }

        // Navigation properties
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<CustomField> CustomFields { get; set; } = new List<CustomField>();

        /// <summary>
        /// Parent certification type (for hierarchical display)
        /// </summary>
        public CertificationType? Parent { get; set; }

        /// <summary>
        /// Child certification types (sub-types)
        /// </summary>
        public ICollection<CertificationType> Children { get; set; } = new List<CertificationType>();
    }
}