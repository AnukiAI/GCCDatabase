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
