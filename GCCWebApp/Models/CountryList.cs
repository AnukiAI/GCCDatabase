using System.Globalization;
using System.Text;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Provides a standard list of countries and territories (ISO 3166-1) for dropdown selection.
    /// </summary>
    public static class CountryList
    {
        // The list below is ISO 3166-1, but it is NOT limited to sovereign states — see DEF-356.
        // It was originally 197 entries = 193 UN members + Vatican City + Palestine + Kosovo +
        // Taiwan. That definition is wrong for this application: Country records where a product
        // was MADE OR TESTED, and manufacturing happens in places that are not sovereign states.
        // The client's own migration data proved it — "Puerto Rico" (2 certs) and "Wales" (6 certs)
        // both arrived from the source files and neither could be re-selected on Edit, which is
        // the DEF-343 failure shape. So:
        //   * ISO 3166-1 territories that are real manufacturing/business locations are included
        //     (Hong Kong, Puerto Rico, Macau, the Crown Dependencies, the Caribbean territories).
        //   * The four UK constituent countries are SELECTABLE rather than collapsed to
        //     "United Kingdom" — the client supplies them, and Location alone was losing the
        //     distinction. This is a deliberate departure from ISO, which treats them as
        //     subdivisions (GB-WLS etc.), taken on the client's instruction 2026-08-14.
        // Sub-national units in general are still NOT countries — "Ontario" and "Alberta" must
        // keep failing to resolve. The UK four are a named exception, not a new rule.
        //
        // Entries are ASCII-only by existing convention ("Ivory Coast", not "Côte d'Ivoire";
        // "Sao Tome and Principe", not "São Tomé"). Keep it that way — this codebase has had
        // mojibake defects in CSV export, and Normalize() strips accents anyway so an accented
        // source value still resolves.
        // The list is kept in alphabetical order because the dropdown renders it verbatim.
        public static readonly List<string> Countries = new()
        {
            "Afghanistan",
            "Albania",
            "Algeria",
            "Andorra",
            "Angola",
            "Antigua and Barbuda",
            "Argentina",
            "Armenia",
            "Aruba",
            "Australia",
            "Austria",
            "Azerbaijan",
            "Bahamas",
            "Bahrain",
            "Bangladesh",
            "Barbados",
            "Belarus",
            "Belgium",
            "Belize",
            "Benin",
            "Bermuda",
            "Bhutan",
            "Bolivia",
            "Bosnia and Herzegovina",
            "Botswana",
            "Brazil",
            "British Virgin Islands",
            "Brunei",
            "Bulgaria",
            "Burkina Faso",
            "Burundi",
            "Cabo Verde",
            "Cambodia",
            "Cameroon",
            "Canada",
            "Cayman Islands",
            "Central African Republic",
            "Chad",
            "Chile",
            "China",
            "Colombia",
            "Comoros",
            "Congo (Democratic Republic)",
            "Congo (Republic)",
            "Costa Rica",
            "Croatia",
            "Cuba",
            "Curacao",
            "Cyprus",
            "Czech Republic",
            "Denmark",
            "Djibouti",
            "Dominica",
            "Dominican Republic",
            "Ecuador",
            "Egypt",
            "El Salvador",
            "England",
            "Equatorial Guinea",
            "Eritrea",
            "Estonia",
            "Eswatini",
            "Ethiopia",
            "Faroe Islands",
            "Fiji",
            "Finland",
            "France",
            "French Polynesia",
            "Gabon",
            "Gambia",
            "Georgia",
            "Germany",
            "Ghana",
            "Gibraltar",
            "Greece",
            "Greenland",
            "Grenada",
            "Guam",
            "Guatemala",
            "Guernsey",
            "Guinea",
            "Guinea-Bissau",
            "Guyana",
            "Haiti",
            "Honduras",
            "Hong Kong",
            "Hungary",
            "Iceland",
            "India",
            "Indonesia",
            "Iran",
            "Iraq",
            "Ireland",
            "Isle of Man",
            "Israel",
            "Italy",
            "Ivory Coast",
            "Jamaica",
            "Japan",
            "Jersey",
            "Jordan",
            "Kazakhstan",
            "Kenya",
            "Kiribati",
            "Kosovo",
            "Kuwait",
            "Kyrgyzstan",
            "Laos",
            "Latvia",
            "Lebanon",
            "Lesotho",
            "Liberia",
            "Libya",
            "Liechtenstein",
            "Lithuania",
            "Luxembourg",
            "Macau",
            "Madagascar",
            "Malawi",
            "Malaysia",
            "Maldives",
            "Mali",
            "Malta",
            "Marshall Islands",
            "Mauritania",
            "Mauritius",
            "Mexico",
            "Micronesia",
            "Moldova",
            "Monaco",
            "Mongolia",
            "Montenegro",
            "Morocco",
            "Mozambique",
            "Myanmar",
            "Namibia",
            "Nauru",
            "Nepal",
            "Netherlands",
            "New Caledonia",
            "New Zealand",
            "Nicaragua",
            "Niger",
            "Nigeria",
            "North Korea",
            "North Macedonia",
            "Northern Ireland",
            "Norway",
            "Oman",
            "Pakistan",
            "Palau",
            "Palestine",
            "Panama",
            "Papua New Guinea",
            "Paraguay",
            "Peru",
            "Philippines",
            "Poland",
            "Portugal",
            "Puerto Rico",
            "Qatar",
            "Romania",
            "Russia",
            "Rwanda",
            "Saint Kitts and Nevis",
            "Saint Lucia",
            "Saint Vincent and the Grenadines",
            "Samoa",
            "San Marino",
            "Sao Tome and Principe",
            "Saudi Arabia",
            "Scotland",
            "Senegal",
            "Serbia",
            "Seychelles",
            "Sierra Leone",
            "Singapore",
            "Sint Maarten",
            "Slovakia",
            "Slovenia",
            "Solomon Islands",
            "Somalia",
            "South Africa",
            "South Korea",
            "South Sudan",
            "Spain",
            "Sri Lanka",
            "Sudan",
            "Suriname",
            "Sweden",
            "Switzerland",
            "Syria",
            "Taiwan",
            "Tajikistan",
            "Tanzania",
            "Thailand",
            "Timor-Leste",
            "Togo",
            "Tonga",
            "Trinidad and Tobago",
            "Tunisia",
            "Turkey",
            "Turkmenistan",
            "Tuvalu",
            "Uganda",
            "Ukraine",
            "United Arab Emirates",
            "United Kingdom",
            "United States",
            "United States Virgin Islands",
            "Uruguay",
            "Uzbekistan",
            "Vanuatu",
            "Vatican City",
            "Venezuela",
            "Vietnam",
            "Wales",
            "Yemen",
            "Zambia",
            "Zimbabwe"
        };

        // DEF-347: CSV import used to write Certificate.Country as a raw string with no check against
        // this list — the UI <select> can't produce an invalid value, so import was the only way in,
        // and it let "Atlantis" into the test-env database. Resolution is deliberately TOLERANT
        // rather than exact-match, following the DEF-276 status precedent (CertificateStatusHelper.
        // TryResolveStatus): a migration file saying "USA" or " united states " is a near-miss to be
        // normalized, not junk to be flagged. Only genuinely unknown values get reported.

        /// <summary>
        /// Common spellings and abbreviations that map onto a canonical <see cref="Countries"/> entry.
        /// Keys are already normalized by <see cref="Normalize"/> (lowercase, punctuation stripped,
        /// whitespace collapsed), so "U.S.A." and "usa" both arrive here as "usa".
        /// </summary>
        private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
        {
            ["us"] = "United States",
            ["usa"] = "United States",
            ["united states of america"] = "United States",
            ["america"] = "United States",
            ["uk"] = "United Kingdom",
            ["great britain"] = "United Kingdom",
            ["britain"] = "United Kingdom",
            // DEF-356: England / Scotland / Wales / Northern Ireland used to alias to
            // "United Kingdom". They are now first-class entries in Countries above (client
            // instruction, 2026-08-14), so they resolve by exact match and must NOT be aliased —
            // an alias here would silently override the client's stated country.
            ["uae"] = "United Arab Emirates",
            ["republic of korea"] = "South Korea",
            ["korea south"] = "South Korea",
            ["dprk"] = "North Korea",
            ["korea north"] = "North Korea",
            ["russia federation"] = "Russia",
            ["russian federation"] = "Russia",
            ["czechia"] = "Czech Republic",
            ["holland"] = "Netherlands",
            ["the netherlands"] = "Netherlands",
            ["cote divoire"] = "Ivory Coast",   // "Côte d'Ivoire" normalises to this key
            ["burma"] = "Myanmar",
            ["vatican"] = "Vatican City",
            ["prc"] = "China",
            ["peoples republic of china"] = "China",
            ["mainland china"] = "China",
            ["roc"] = "Taiwan",
            ["viet nam"] = "Vietnam",
            ["cape verde"] = "Cabo Verde",
            ["swaziland"] = "Eswatini",
            ["macedonia"] = "North Macedonia",

            // DEF-356 additions.
            // ISO renamed Turkey to "Türkiye" in 2022, so current documents use it. Normalize()
            // strips the umlaut, giving the key below — without this a modern source file silently
            // fails to resolve.
            ["turkiye"] = "Turkey",
            ["holy see"] = "Vatican City",     // the ISO 3166-1 name for Vatican City
            ["east timor"] = "Timor-Leste",
            ["republic of ireland"] = "Ireland",
            ["eire"] = "Ireland",
            // Territories added to Countries above; these cover the forms customs paperwork uses.
            ["hong kong sar"] = "Hong Kong",
            ["hong kong sar china"] = "Hong Kong",
            ["hong kong china"] = "Hong Kong",
            ["macao"] = "Macau",               // ISO spells it "Macao"
            ["macao sar"] = "Macau",
            ["macau sar"] = "Macau",
            ["us virgin islands"] = "United States Virgin Islands",
            ["virgin islands us"] = "United States Virgin Islands",   // ISO "Virgin Islands (U.S.)"
            ["usvi"] = "United States Virgin Islands",
            ["virgin islands british"] = "British Virgin Islands",    // ISO "Virgin Islands (British)"
            ["bvi"] = "British Virgin Islands",
            ["channel islands jersey"] = "Jersey",
            ["puerto rico us"] = "Puerto Rico",
        };

        // Built once from Countries so a rename in the list above can never drift out of sync with
        // the lookup — do not hand-maintain a parallel copy.
        private static readonly Dictionary<string, string> CanonicalByNormalized =
            Countries.ToDictionary(Normalize, c => c, StringComparer.Ordinal);

        /// <summary>
        /// Lowercase, strip accents and punctuation, collapse whitespace. "U.S.A. " → "usa";
        /// "  UNITED   STATES " → "united states"; "Côte d'Ivoire" → "cote divoire".
        /// </summary>
        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            // Decompose so accents become separate combining marks we can drop — a migration file
            // saying "Côte d'Ivoire" must land on the same key as "Cote dIvoire".
            var decomposed = value.Trim().Normalize(NormalizationForm.FormD);

            var chars = new List<char>(decomposed.Length);
            var lastWasSpace = false;
            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsWhiteSpace(ch))
                {
                    if (chars.Count > 0 && !lastWasSpace) { chars.Add(' '); lastWasSpace = true; }
                    continue;
                }
                // Keep letters/digits only; apostrophes, periods, hyphens and commas are noise here
                // ("Cote d'Ivoire" and "Cote dIvoire" must land on the same key).
                if (char.IsLetterOrDigit(ch)) { chars.Add(char.ToLowerInvariant(ch)); lastWasSpace = false; }
            }

            // Trailing separator can survive the loop when the value ends in punctuation.
            while (chars.Count > 0 && chars[^1] == ' ') chars.RemoveAt(chars.Count - 1);
            return new string(chars.ToArray());
        }

        /// <summary>
        /// Resolves a free-text country value to its canonical <see cref="Countries"/> spelling,
        /// tolerating case, surrounding whitespace, internal punctuation and common aliases.
        /// Returns false for blank input and for values that match nothing.
        /// </summary>
        public static bool TryResolve(string? value, out string canonical)
        {
            canonical = string.Empty;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var key = Normalize(value);
            if (key.Length == 0) return false;

            if (CanonicalByNormalized.TryGetValue(key, out var exact)) { canonical = exact; return true; }
            if (Aliases.TryGetValue(key, out var aliased)) { canonical = aliased; return true; }

            return false;
        }

        /// <summary>True if the value resolves to a known country. Blank is NOT valid — callers that
        /// allow an empty country must check for blank themselves, since Country is optional.</summary>
        public static bool IsKnown(string? value) => TryResolve(value, out _);
    }
}
