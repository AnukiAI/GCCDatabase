using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    /// <summary>
    /// CR-037: one row per country a certificate is associated with.
    ///
    /// Deliberately a <b>string</b> join table (CR-037 Option A), not a true many-to-many against a
    /// seeded Countries entity. Country has no properties of its own — no IsActive, no SortOrder, no
    /// login association — so unlike <see cref="Partner"/> there is nothing to model as an entity, and
    /// no admin screen or seed data is implied. The picker is still fed by the static
    /// <see cref="CountryList.Countries"/>.
    ///
    /// <see cref="Certificate.Country"/> is retained as the denormalized <i>primary</i> country exactly
    /// as CR-171 retained the legacy Partner string: it remains the sort key, the CSV column and the
    /// value the non-partner API projects. This table holds the full set, primary included, so a
    /// membership test never has to union the two.
    /// </summary>
    public class CertificateCountry
    {
        public int Id { get; set; }

        public int CertificateId { get; set; }

        /// <summary>
        /// Width must match <see cref="Certificate.Country"/> at nvarchar(100). A narrower
        /// denormalized copy silently truncates on write — the DEF-314 failure.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// CR-187 persisted computed column, same expression as
        /// <see cref="Certificate.NormalizedCountry"/>. Present so that secondary countries are
        /// reachable by the same fuzzy search as the primary — without it, searching "united states"
        /// would find a certificate whose primary is the US but miss one where it is the second
        /// country. Read-only in code; SQL maintains it on every write.
        /// </summary>
        public string? NormalizedCountry { get; private set; }

        public Certificate? Certificate { get; set; }
    }
}
