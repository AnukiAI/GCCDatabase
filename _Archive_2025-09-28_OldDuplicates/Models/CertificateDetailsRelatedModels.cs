using System;

namespace GreenCircleCertified.ViewModels
{
    public class CertificateTag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsExternal { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CertificationField
    {
        public int Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldValue { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class CSFAttribute
    {
        public int Id { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeValue { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsPubliclyVisible { get; set; }
    }

    public class AuditTrailEntry
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
    }

    public class RelatedDocument
    {
        public int Id { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public bool IsPubliclyAccessible { get; set; }
    }

    public class InternalNote
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string Priority { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
    }
}
