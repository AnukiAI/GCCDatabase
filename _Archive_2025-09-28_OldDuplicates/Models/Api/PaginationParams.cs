using System.ComponentModel.DataAnnotations;
using GCCWebApp.Validation;

namespace GCCWebApp.Models.Api
{
    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        [StringLength(50, ErrorMessage = "Sort field name must not exceed 50 characters")]
        public string? SortBy { get; set; }
        
        public bool SortDescending { get; set; } = false;
        
        [StringLength(100, ErrorMessage = "Search term must not exceed 100 characters")]
        [InputSanitization(allowHtml: false, allowSqlKeywords: false, allowScriptTags: false)]
        public string? SearchTerm { get; set; }
    }
}