using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Search
{
    /// <summary>
    /// DEF-409 — relevance ranking for certificate search.
    ///
    /// THE DEFECT
    /// Searching "23" returned 1,219 rows of which 1,075 really were 23-xxxx, but the first one sat
    /// at position 67 — page 4 at the UI's 20/page. Above it were 26-0623, 26-0523, 26-0323: rows
    /// that genuinely contain "23", in the SEQUENCE half rather than the year half. The default sort
    /// is newest-first, so 2026 certificates outranked 2023 ones and the user read three pages of
    /// apparent nonsense before the first correct hit. The filter was right; the order was wrong.
    ///
    /// WHY A PREFIX MATCH IS THE RIGHT SIGNAL
    /// SearchNormalizer strips '-', so "23-0456" is stored as "230456". A prefix match on the
    /// normalized number is therefore exactly a year-prefix match, with no parsing of the number
    /// format and no special case for the three certificates that are 6H3-NNNN rather than YY-NNNN.
    /// NormalizedNumber is already indexed for precisely this ("Indexed for prefix/autocomplete",
    /// ApplicationDbContext.cs:132).
    ///
    /// TWO BANDS, NOT THREE
    /// An "exact match" band would be redundant here. Verified against the test database
    /// 2026-09-05: **zero** stored normalized numbers are a strict prefix of a different one
    /// (lengths are 6 or 7, and no 6-char value prefixes a 7-char one). So a full-number query
    /// already lands alone in the prefix band, and a third band would sort nothing.
    ///
    /// RANKING IS NOT FILTERING
    /// Every row the search matched is still returned, and totalCount is unchanged. Nothing becomes
    /// undiscoverable — 26-0623 is still found by searching "23", just below the 2023 rows. That
    /// property is the whole reason this approach was chosen over anchoring "23" to "23-%", which
    /// would have silently stopped returning real records (6H3-5692 is findable today by searching
    /// "35"; under anchoring it would not be).
    ///
    /// WHERE THE ORDERING GOES
    /// In the ORDER BY that follows the filter — never inside the search OR. DEF-402 is the standing
    /// warning: a clause added there cost 15x.
    /// </summary>
    public static class SearchRanking
    {
        /// <summary>Rows whose certificate number begins with the normalized query — the top band.</summary>
        public static Expression<Func<Certificate, bool>> NumberPrefixMatch(string nq) =>
            c => c.NormalizedNumber != null && c.NormalizedNumber.StartsWith(nq);

        /// <summary>
        /// The complement of <see cref="NumberPrefixMatch"/>, written out rather than derived by
        /// negating the expression tree — an explicit inverse is easier to read and cannot be
        /// mistranslated. NULL is grouped into the lower band (there are none today, but the column
        /// is nullable).
        /// </summary>
        public static Expression<Func<Certificate, bool>> NotNumberPrefixMatch(string nq) =>
            c => c.NormalizedNumber == null || !c.NormalizedNumber.StartsWith(nq);

        /// <summary>
        /// A page of certificate ids, prefix matches first, with <paramref name="applySort"/>
        /// governing the order WITHIN each band.
        ///
        /// Implemented as two banded queries rather than by prepending a sort key, because
        /// ApplySorting is a 33-branch switch returning IOrderedQueryable and a leading key cannot
        /// be prepended to an already-ordered query. Partitioning leaves all 33 branches untouched,
        /// which is where the risk actually lives.
        ///
        /// With no query term this is byte-for-byte the original call — one sorted, paged, id-only
        /// projection — so unsearched listings are provably unaffected.
        /// </summary>
        public static async Task<List<int>> PageIdsAsync(
            IQueryable<Certificate> query,
            Func<IQueryable<Certificate>, IQueryable<Certificate>> applySort,
            string? nq,
            int offset,
            int limit)
        {
            if (string.IsNullOrEmpty(nq))
            {
                return await applySort(query)
                    .Skip(offset).Take(limit)
                    .Select(c => c.Id)
                    .ToListAsync();
            }

            var top = query.Where(NumberPrefixMatch(nq));
            var rest = query.Where(NotNumberPrefixMatch(nq));

            // COUNT the SORTED query, not the raw band. One sort branch ("recently-issued") adds a
            // .Where() inside the switch — a filter wearing a sort's clothing. Counting `top`
            // directly would overcount it and the page boundary would then skip rows.
            var topCount = await applySort(top).CountAsync();

            var ids = new List<int>(limit);

            if (offset < topCount)
            {
                var take = Math.Min(limit, topCount - offset);
                ids.AddRange(await applySort(top)
                    .Skip(offset).Take(take)
                    .Select(c => c.Id)
                    .ToListAsync());
            }

            var remaining = limit - ids.Count;
            if (remaining > 0)
            {
                // Once the top band is exhausted the lower band continues where it left off.
                var restOffset = Math.Max(0, offset - topCount);
                ids.AddRange(await applySort(rest)
                    .Skip(restOffset).Take(remaining)
                    .Select(c => c.Id)
                    .ToListAsync());
            }

            return ids;
        }

        /// <summary>
        /// The whole result set, ranked — for callers that do not page (an export, a search that
        /// returns everything). Same banding; the two halves are concatenated in order.
        /// </summary>
        public static async Task<List<Certificate>> ToRankedListAsync(
            IQueryable<Certificate> query,
            Func<IQueryable<Certificate>, IQueryable<Certificate>> applySort,
            string? nq)
        {
            if (string.IsNullOrEmpty(nq))
            {
                return await applySort(query).ToListAsync();
            }

            var top = await applySort(query.Where(NumberPrefixMatch(nq))).ToListAsync();
            var rest = await applySort(query.Where(NotNumberPrefixMatch(nq))).ToListAsync();
            top.AddRange(rest);
            return top;
        }

        /// <summary>
        /// Ranked ids for a capped, unsorted caller — the autocomplete endpoints, which take the
        /// first N matches with no ORDER BY at all. Prefix matches first is the entire fix there:
        /// a suggestion list offering "26-0623" when the user has typed "23" is the sharpest form
        /// of this defect, and the cheapest to correct.
        /// </summary>
        public static IQueryable<Certificate> OrderByNumberRelevance(
            this IQueryable<Certificate> query, string nq) =>
            query
                .OrderBy(c => c.NormalizedNumber != null && c.NormalizedNumber.StartsWith(nq) ? 0 : 1)
                .ThenBy(c => c.Number)
                .ThenBy(c => c.Id);
    }
}
