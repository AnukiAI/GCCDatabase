using System.Globalization;

namespace GCCWebApp.Extensions
{
    public static class DisplayFormatters
    {
        // CertificationResults is free-text (analysts may type "0.17", "17%", "Pass", ">50%", etc.).
        // Client requirement: display as percentage. Rule:
        //   - null / empty          → unchanged
        //   - already contains '%'  → unchanged (already formatted)
        //   - not numeric           → unchanged (e.g. "Pass", "Silver")
        //   - numeric in [0, 1]     → multiply by 100, append '%'  (0.17 → "17%", 1 → "100%")
        //   - numeric > 1           → append '%'                   (17 → "17%")
        //
        // DEF-383: the boundary is INCLUSIVE of 1. It was `< 1` from this file's first commit, which
        // sent the single value `1` down the "already a whole percentage" branch and rendered it as
        // "1%". In the client's data `1` means 100% — proved by enumerating every distinct stored
        // value: the scale runs 0.01 (6 certs) through 0.99 (24 certs) and then 1 (106 certs), with
        // NO value anywhere between 1 and 100. If `1` meant one percent it would collide with the
        // 0.01 already used for that, and 106 certificates would be recorded as the worst results in
        // the dataset rather than the best.
        //
        // Nothing regressed here — the rule never handled its own boundary. It only became visible
        // when the client's real data was migrated in, because `1` is now the most common value in
        // the field (106 of 988 populated).
        //
        // KEEP IN SYNC with the CertificationResultsNumeric computed column
        // (ApplicationDbContext + Migrations/…_DEF383FixCertResultsPercentBoundary.cs). That column
        // drives the numeric SORT; if the two boundaries disagree, these 106 certificates display
        // "100%" while sorting as though they were 1%.
        public static string? FormatCertificationResults(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var trimmed = value.Trim();
            if (trimmed.Contains('%')) return value;

            if (!decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return value;

            var pct = number >= 0 && number <= 1 ? number * 100 : number;
            return pct.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }
    }
}
