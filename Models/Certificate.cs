using System;
using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        [Required]
        public string Number { get; set; }

        [Required]
        public string Type { get; set; }

        public string Company { get; set; }
        public string Product { get; set; }
        public string Category { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
    }
}