using System.Text;

namespace GCCWebApp.Search
{
    /// <summary>
    /// CR-187: single source of truth for user-friendly search normalization.
    ///
    /// Search must treat "Product A", "Product™ A", "product-a" and "Product:  A" as the same
    /// thing. We achieve that by normalizing BOTH sides of the comparison to a canonical form:
    /// lowercase, with trademark/registered/copyright symbols, punctuation, and all whitespace
    /// removed (so "product a" -> "producta").
    ///
    /// The stored side of the comparison is a persisted computed column on each searchable entity
    /// (see ApplicationDbContext + Models.*.NormalizedName). The search-input side is normalized in
    /// C# via <see cref="Normalize"/>. To guarantee the two can never drift, the SQL for the
    /// computed columns is GENERATED from the same <see cref="RemovableChars"/> array via
    /// <see cref="ToSqlExpression"/> — change the rule in one place and both halves follow.
    /// </summary>
    public static class SearchNormalizer
    {
        /// <summary>
        /// The canonical set of characters stripped during normalization. Letters and digits are
        /// always kept (and lowercased); everything listed here is removed. Both the C# and SQL
        /// normalization paths are built from this array, so they stay in lockstep.
        /// </summary>
        private static readonly char[] RemovableChars =
        {
            // whitespace (collapses "Product   A" and "Product A" to the same value)
            ' ', '\t', '\n', '\r',
            // trademark / registered / copyright / service-mark family
            '™', // ™
            '®', // ®
            '©', // ©
            '℠', // ℠
            '°', // °
            // dashes / hyphens
            '-', '–', '—', // - – —
            // common punctuation & separators
            '_', '.', ',', ':', ';', '!', '?',
            '\'', '‘', '’', '`', // ' ‘ ’ `
            '"', '“', '”',       // " “ ”
            '(', ')', '[', ']', '{', '}',
            '/', '\\', '|', '&', '+', '*', '@', '#', '~', '^', '=', '<', '>',
            // LIKE wildcard '%' — stripped from both sides so it can never act as a wildcard
            // ('_' is already removed above, covering the other LIKE wildcard).
            '%'
        };

        /// <summary>
        /// Normalizes a search string for comparison: lowercases and removes every character in
        /// <see cref="RemovableChars"/>. Null/empty input returns an empty string.
        /// E.g. "Product™ A" and "product-a" both become "producta".
        /// </summary>
        public static string Normalize(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (IsRemovable(ch)) continue;
                sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        private static bool IsRemovable(char ch)
        {
            for (int i = 0; i < RemovableChars.Length; i++)
            {
                if (RemovableChars[i] == ch) return true;
            }
            return false;
        }

        /// <summary>
        /// Builds the deterministic SQL Server expression that mirrors <see cref="Normalize"/> for a
        /// given column reference, e.g. ToSqlExpression("[Name]") =>
        /// LOWER(REPLACE(REPLACE([Name], N' ', N''), ...)). Used by the EF computed-column config so
        /// the stored normalized value matches what Normalize produces for the search input.
        /// The expression is deterministic (LOWER/REPLACE only), so the column can be PERSISTED and
        /// indexed.
        /// </summary>
        public static string ToSqlExpression(string columnSql)
        {
            // De-duplicate (RemovableChars intentionally lists '_' twice for documentation clarity).
            var seen = new HashSet<char>();
            var expr = columnSql;
            foreach (var ch in RemovableChars)
            {
                if (!seen.Add(ch)) continue;
                expr = $"REPLACE({expr}, {SqlCharLiteral(ch)}, N'')";
            }
            return $"LOWER({expr})";
        }

        private static string SqlCharLiteral(char ch) => ch switch
        {
            '\t' => "NCHAR(9)",
            '\n' => "NCHAR(10)",
            '\r' => "NCHAR(13)",
            '\'' => "N''''",
            _ => $"N'{ch}'"
        };
    }
}
