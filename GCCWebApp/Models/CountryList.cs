using System.Globalization;
using System.Text;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Provides a standard list of countries (ISO 3166-1) for dropdown selection.
    /// </summary>
    public static class CountryList
    {
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
            "Bhutan",
            "Bolivia",
            "Bosnia and Herzegovina",
            "Botswana",
            "Brazil",
            "Brunei",
            "Bulgaria",
            "Burkina Faso",
            "Burundi",
            "Cabo Verde",
            "Cambodia",
            "Cameroon",
            "Canada",
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
            "Cyprus",
            "Czech Republic",
            "Denmark",
            "Djibouti",
            "Dominica",
            "Dominican Republic",
            "Ecuador",
            "Egypt",
            "El Salvador",
            "Equatorial Guinea",
            "Eritrea",
            "Estonia",
            "Eswatini",
            "Ethiopia",
            "Fiji",
            "Finland",
            "France",
            "Gabon",
            "Gambia",
            "Georgia",
            "Germany",
            "Ghana",
            "Greece",
            "Grenada",
            "Guatemala",
            "Guinea",
            "Guinea-Bissau",
            "Guyana",
            "Haiti",
            "Honduras",
            "Hungary",
            "Iceland",
            "India",
            "Indonesia",
            "Iran",
            "Iraq",
            "Ireland",
            "Israel",
            "Italy",
            "Ivory Coast",
            "Jamaica",
            "Japan",
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
            "New Zealand",
            "Nicaragua",
            "Niger",
            "Nigeria",
            "North Korea",
            "North Macedonia",
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
            "Senegal",
            "Serbia",
            "Seychelles",
            "Sierra Leone",
            "Singapore",
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
            "Uruguay",
            "Uzbekistan",
            "Vanuatu",
            "Vatican City",
            "Venezuela",
            "Vietnam",
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
            ["england"] = "United Kingdom",
            ["scotland"] = "United Kingdom",
            ["wales"] = "United Kingdom",
            ["northern ireland"] = "United Kingdom",
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
