using System;
using System.Linq;
using System.Linq.Expressions;
using GCCWebApp.Models;

namespace GCCWebApp.Extensions
{
    /// <summary>
    /// Shared sort helpers so the public, external, and internal certificate tile queries
    /// order identically. QC #157 (client, 5/29/2026) — the client asked that empty/blank
    /// values always sort to the END regardless of direction, "consistent across the
    /// application." SQL Server's default places NULL first in ascending, so sorting a
    /// sparsely-populated column (Location, ASIN, Company URL, Cert Subcategories, etc.)
    /// ascending used to fill the first page with blanks and read as "not sorting." With
    /// these helpers, populated values always lead; null/empty always trail — both directions.
    /// </summary>
    public static class CertificateSortExtensions
    {
        // Primary order is an (isEmpty) bit: false(0) first, true(1) last. That term is never
        // reversed, so blanks stay last whether the value sort is asc or desc. Then the value,
        // then a stable .ThenBy(Id) is expected to be appended by the caller (every existing
        // branch already does this for deterministic pagination — see DEF-166 history).
        public static IOrderedQueryable<Certificate> OrderByTextEmptiesLast(
            this IQueryable<Certificate> query,
            Expression<Func<Certificate, string?>> key,
            bool descending)
        {
            var param = key.Parameters[0];
            // (key ?? "") == ""   → EF emits ORDER BY CASE WHEN COALESCE(col,'') = '' THEN 1 ELSE 0 END
            var isEmpty = Expression.Equal(
                Expression.Coalesce(key.Body, Expression.Constant(string.Empty)),
                Expression.Constant(string.Empty));
            var emptyFlag = Expression.Lambda<Func<Certificate, bool>>(isEmpty, param);

            var ordered = query.OrderBy(emptyFlag);
            return descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
        }

        // Natural-ish sort for identifier columns (ASIN/UPC/GTIN/EAN). A plain lexicographic text
        // sort orders numeric identifiers wrong: "100" sorts next to "10"/"1", so a column of
        // 1,2,…,100 reads as 1,10,100,2,… (and in DESC, "100" trails the single digits — the client's
        // "100 after single digits" report). Ordering by VALUE LENGTH first, then the value, fixes the
        // common case: "9"(len 1) < "10"(len 2) < "100"(len 3), and within a single length the lexical
        // order already equals the numeric order. Real IDs are fixed-length per type (ASIN=10, UPC=12,
        // EAN=13, GTIN=14), so for production data the length term is constant and ordering degrades to
        // the prior lexical behavior — alphanumeric ASINs (e.g. B0BRNN8BKS) still sort correctly.
        // Empty/blank keys are still forced LAST in both directions (QC #157), same as the text helper.
        public static IOrderedQueryable<Certificate> OrderByNaturalEmptiesLast(
            this IQueryable<Certificate> query,
            Expression<Func<Certificate, string?>> key,
            bool descending)
        {
            var param = key.Parameters[0];
            var coalesced = Expression.Coalesce(key.Body, Expression.Constant(string.Empty));

            var isEmpty = Expression.Equal(coalesced, Expression.Constant(string.Empty));
            var emptyFlag = Expression.Lambda<Func<Certificate, bool>>(isEmpty, param);

            // (key ?? "").Length → EF emits ORDER BY ... , LEN(COALESCE(col,'')) , col
            var lengthExpr = Expression.Property(coalesced, nameof(string.Length));
            var lengthKey = Expression.Lambda<Func<Certificate, int>>(lengthExpr, param);

            var ordered = query.OrderBy(emptyFlag);
            return descending
                ? ordered.ThenByDescending(lengthKey).ThenByDescending(key)
                : ordered.ThenBy(lengthKey).ThenBy(key);
        }

        // Count counterpart (ASIN/UPC/GTIN/EAN columns): the Cert List renders these cells as a
        // COUNT badge (how many IDs the cert has), not the ID values — so the client expects the
        // column to sort by that visible count (DEF-260, QC #157: "1000 should be at the top or
        // bottom"). Orders by the count; cells with no IDs (count 0) are forced LAST in both
        // directions, consistent with the blanks-last rule the text/numeric helpers apply.
        public static IOrderedQueryable<Certificate> OrderByCountEmptiesLast(
            this IQueryable<Certificate> query,
            Expression<Func<Certificate, int>> countKey,
            bool descending)
        {
            var param = countKey.Parameters[0];
            // (count == 0) → empties last. That term is never reversed, so 0-count rows stay last
            // whether the count sort is asc or desc.
            var isEmpty = Expression.Equal(countKey.Body, Expression.Constant(0));
            var emptyFlag = Expression.Lambda<Func<Certificate, bool>>(isEmpty, param);

            var ordered = query.OrderBy(emptyFlag);
            return descending ? ordered.ThenByDescending(countKey) : ordered.ThenBy(countKey);
        }

        // Numeric counterpart (Cert Results): NULLs always last in both directions. This
        // supersedes the DEF-181 choice of "NULLs first in ASC" for cert-results, per the
        // client's "consistent across the application" direction — populated values lead.
        public static IOrderedQueryable<Certificate> OrderByNumericNullsLast(
            this IQueryable<Certificate> query,
            Expression<Func<Certificate, decimal?>> key,
            bool descending)
        {
            var param = key.Parameters[0];
            var isNull = Expression.Equal(key.Body, Expression.Constant(null, typeof(decimal?)));
            var nullFlag = Expression.Lambda<Func<Certificate, bool>>(isNull, param);

            var ordered = query.OrderBy(nullFlag);
            return descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
        }
    }
}
