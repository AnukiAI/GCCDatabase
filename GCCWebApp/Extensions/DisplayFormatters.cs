using System.Globalization;

namespace GCCWebApp.Extensions
{
    public static class DisplayFormatters
    {
        // CertificationResults is free-text (analysts may type "0.17", "17%", "Pass", ">50%", etc.).
        // Client requirement: display as percentage. Rule:
        //   - null / empty          → unchanged
        //   - already contains '%'  → unchanged (already formatted)
        //   - not numeric           → unchanged (e.g. "Pass")
        //   - numeric in [0, 1)     → multiply by 100, append '%'  (0.17 → "17%")
        //   - numeric ≥ 1           → append '%'                   (17 → "17%")
        public static string? FormatCertificationResults(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var trimmed = value.Trim();
            if (trimmed.Contains('%')) return value;

            if (!decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return value;

            var pct = number >= 0 && number < 1 ? number * 100 : number;
            return pct.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }
    }
}
