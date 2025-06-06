using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Location { get; set; }
        public string Country { get; set; }
    }
}