using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    /// <summary>
    /// ViewModel for Certificate data based on GCC Database Field Requirements
    /// Contains all fields from the Cert Upload Fields specification
    /// </summary>
    public class CertificateViewModel
    {
        #region Core Certificate Information

        /// <summary>
        /// Certificate Number - Format: XX-XXXX (auto-generated if blank)
        /// </summary>
        [Display(Name = "Certificate #")]
        public required string CertificateNumber { get; set; }

        /// <summary>
        /// Type of certification issued
        /// </summary>
        [Required]
        [Display(Name = "Certification Type")]
        public required string CertificationType { get; set; }

        /// <summary>
        /// Multi-select CSF Attributes
        /// </summary>
        [Display(Name = "CSF Attributes")]
        public required string CSFAttributes { get; set; }

        /// <summary>
        /// Certificate status - determines public visibility
        /// </summary>
        [Required]
        [Display(Name = "Status")]
        public required string Status { get; set; }

        /// <summary>
        /// When the certification becomes effective
        /// </summary>
        [Required]
        [Display(Name = "Effective Date")]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; }

        /// <summary>
        /// When the certification expires
        /// </summary>
        [Display(Name = "Expiration Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpirationDate { get; set; }

        #endregion

        #region Company Information

        /// <summary>
        /// Company name (dropdown with add capability)
        /// </summary>
        [Required]
        [Display(Name = "Company")]
        public required string CompanyName { get; set; }

        /// <summary>
        /// Optional company contact email or URL
        /// </summary>
        [Display(Name = "Company Contact Email/URL")]
        public required string CompanyContactEmail { get; set; }

        /// <summary>
        /// Optional company website URL
        /// </summary>
        [Display(Name = "Company URL")]
        public required string CompanyContactURL { get; set; }

        #endregion

        #region Product Information

        /// <summary>
        /// Product name (dropdown with add capability)
        /// </summary>
        [Required]
        [Display(Name = "Product Name")]
        public required string ProductName { get; set; }

        /// <summary>
        /// Product category (auto-populated from product)
        /// </summary>
        [Required]
        [Display(Name = "Product Category")]
        public string? ProductCategory { get; set; }

        /// <summary>
        /// Product type (sub-level to category)
        /// </summary>
        [Display(Name = "Product Type")]
        public string? ProductType { get; set; }

        /// <summary>
        /// Product type subcategory (level 3)
        /// </summary>
        [Display(Name = "Product Type Subcategory")]
        public string? ProductTypeSubcategory { get; set; }

        #endregion

        #region Location Information

        /// <summary>
        /// Location (up to 175 characters, multiple locations allowed)
        /// </summary>
        [Display(Name = "Location")]
        [StringLength(175)]
        public string? Location { get; set; }

        /// <summary>
        /// Country (required for search filtering and Amazon)
        /// </summary>
        [Required]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        #endregion

        #region Tags and Classification

        /// <summary>
        /// External tags (multi-select, public visible)
        /// </summary>
        [Display(Name = "External Tag")]
        public string? ExternalTag { get; set; }

        /// <summary>
        /// Internal tags (multi-select, login only)
        /// </summary>
        [Display(Name = "Internal Tag")]
        public string? InternalTag { get; set; }

        /// <summary>
        /// Partner designation (Amazon/Novi/None/Other)
        /// </summary>
        [Display(Name = "Partner")]
        public string? Partner { get; set; }

        /// <summary>
        /// Optional test results or scores for partner requirements (e.g., Amazon threshold verification)
        /// </summary>
        [Display(Name = "Certification Results")]
        [StringLength(1000)]
        public string? CertificationResults { get; set; }

        #endregion

        #region Product Identifiers

        /// <summary>
        /// Amazon Standard Identification Number
        /// </summary>
        [Display(Name = "ASIN")]
        public string? ASIN { get; set; }

        /// <summary>
        /// Universal Product Code
        /// </summary>
        [Display(Name = "UPC")]
        public string? UPC { get; set; }

        /// <summary>
        /// Global Trade Item Number
        /// </summary>
        [Display(Name = "GTIN")]
        public string? GTIN { get; set; }

        /// <summary>
        /// European Article Number
        /// </summary>
        [Display(Name = "EAN")]
        public string? EAN { get; set; }

        #endregion

        #region File Management

        /// <summary>
        /// Path to uploaded certificate file
        /// </summary>
        [Display(Name = "Certificate Upload")]
        public string? CertificateUploadPath { get; set; }

        /// <summary>
        /// URL for certificate verification
        /// </summary>
        [Display(Name = "Verification URL")]
        public string? VerificationURL { get; set; }

        #endregion

        #region Record Management

        /// <summary>
        /// When the record was created (auto-generated)
        /// </summary>
        [Display(Name = "Record Created Date")]
        public DateTime RecordCreatedDate { get; set; }

        /// <summary>
        /// When the record was last updated (auto-generated)
        /// </summary>
        [Display(Name = "Record Last Updated Date")]
        public DateTime RecordLastUpdatedDate { get; set; }

        /// <summary>
        /// User who created the record
        /// </summary>
        [Display(Name = "Created By")]
        public required string CreatedBy { get; set; }

        /// <summary>
        /// User who last edited the record (auto-generated)
        /// </summary>
        [Display(Name = "Last Edited By")]
        public required string LastEditedBy { get; set; }

        #endregion
    }
}