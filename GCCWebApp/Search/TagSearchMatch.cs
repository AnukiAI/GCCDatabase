using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Models;

namespace GCCWebApp.Search
{
    /// <summary>
    /// DEF-366: makes the tag half of certificate search cheap.
    ///
    /// THE PROBLEM
    /// Every certificate search predicate is a big OR across the searchable fields, and the last
    /// term used to be the tag match:
    ///
    ///     c.NormalizedNumber.Contains(nq) || c.Company.NormalizedName.Contains(nq) || …
    ///         || c.Tags.Any(t => t.NormalizedName.Contains(nq))      // <- this one
    ///
    /// Because the tag clause is OR'd rather than AND'd, SQL Server cannot collapse it into a
    /// semi-join against the CertificateTag/Tags pair. It has to evaluate the correlated subquery
    /// once per row of the already-joined Certificates x Companies x Products x CertificationTypes
    /// result. Measured on the test database (5,702 certificates, 4 tag rows, 412 tag links):
    ///
    ///     paged search WITH the tag clause in the OR      3,200 ms
    ///     identical query with the tag clause removed       212 ms
    ///     tag-matched certificate ids resolved on their own    0 ms
    ///
    /// On the dev clone the same clause produced 17,106 logical reads against a FOUR ROW table —
    /// the per-row re-execution in plain sight. The cost is paid twice per request (the CountAsync
    /// and the paged fetch), which is what made search take 5-60 seconds after the 5,702-row
    /// migration load. At 55 certificates the same clause was invisible.
    ///
    /// The cost also correlates INVERSELY with the number of matches: a term that matches many
    /// rows lets the OFFSET/FETCH row goal stop early, while a rare or non-matching term forces
    /// the full evaluation. That is why the slowness looked erratic rather than constant.
    ///
    /// THE FIX
    /// Resolve the tag-matched certificate ids up front — as its own query, where it IS a plain
    /// semi-join and costs nothing — then either
    ///   * drop the tag clause entirely when nothing matched (the overwhelmingly common case,
    ///     since tag names are few and short), or
    ///   * OR in a set-membership test against that id list.
    ///
    /// Rejected alternatives, both measured on the test database and both no help, so don't
    /// re-try them: rewriting the clause as an uncorrelated `c.Id IN (SELECT …)` (2,539 ms) and
    /// OPTION (RECOMPILE, USE HINT('DISABLE_OPTIMIZER_ROWGOAL')) (2,651 ms). The problem is the
    /// OR, not the plan shape or the row goal.
    /// </summary>
    public static class TagSearchMatch
    {
        /// <summary>
        /// Above this many matched ids we fall back to leaving the tag clause out of the id-list
        /// form and use a subquery instead, so we never emit a pathological literal list. Today
        /// the ceiling is 412 (every certificate that carries any tag at all), so this is headroom
        /// rather than a live limit — but tags are user-created, so the guard has to exist.
        /// </summary>
        public const int MaxInlineIds = 2000;

        /// <summary>
        /// Certificate ids whose tags match the already-normalized search term <paramref name="nq"/>.
        /// Runs as a standalone semi-join (measured 0 ms against the 5,702-row test database).
        /// Returns an empty list for a blank term.
        /// </summary>
        public static async Task<IReadOnlyList<int>> MatchingCertificateIdsAsync(
            IQueryable<Certificate> certificates,
            string nq,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nq)) return Array.Empty<int>();

            return await certificates
                .Where(c => c.Tags.Any(t => t.NormalizedName!.Contains(nq)))
                .Select(c => c.Id)
                // OrderBy is not cosmetic: these ids are emitted as a literal IN list by
                // WhereMatchesSearch, so the id ORDER is part of the SQL text. Without a stable
                // order SQL Server can hand them back differently between calls, every call
                // becomes a plan-cache miss, and the search recompiles a 10-join query every
                // time — turning this fix into a regression.
                .OrderBy(id => id)
                .Take(MaxInlineIds + 1)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Applies a certificate search: the caller supplies the OR across the plain (non-tag)
        /// searchable fields, and this method OR's the tag match back in — cheaply.
        ///
        /// Call sites keep their own field lists because they genuinely differ (the public tile
        /// API searches Product.Category, the paged repository query does not, and so on). Only
        /// the tag half is shared, because only the tag half was the performance problem.
        /// </summary>
        public static IQueryable<Certificate> WhereMatchesSearch(
            this IQueryable<Certificate> query,
            Expression<Func<Certificate, bool>> fieldPredicate,
            IReadOnlyList<int> tagMatchedIds)
        {
            // Nothing matched on tags → the tag clause cannot change the result set, so leaving it
            // out is not an optimization that trades away correctness. This is the fast path and
            // the common one.
            if (tagMatchedIds.Count == 0)
                return query.Where(fieldPredicate);

            Expression<Func<Certificate, bool>> tagPredicate;
            if (tagMatchedIds.Count <= MaxInlineIds)
            {
                // EF.Constant per the DEF-328 convention already used in CertificateRepository:
                // a literal IN list lets the optimizer do histogram lookups on the real values
                // instead of guessing at OPENJSON(@param) cardinality. The distinct id-sets are
                // bounded by the number of distinct tag-name matches, so plan-cache churn is
                // bounded too.
                tagPredicate = c => EF.Constant(tagMatchedIds).Contains(c.Id);
            }
            else
            {
                // Over the guard: don't emit a huge literal list. A parameterized Contains costs
                // an OPENJSON cardinality guess, which is still far better than the per-row
                // correlated subquery this class exists to remove.
                var ids = tagMatchedIds.ToList();
                tagPredicate = c => ids.Contains(c.Id);
            }

            // OR the two lambdas together against a single parameter. Building the expression by
            // hand (rather than `x => field(x) || tag(x)`) keeps it translatable — an invoked
            // lambda is not.
            var parameter = fieldPredicate.Parameters[0];
            var tagBody = new ParameterRebinder(tagPredicate.Parameters[0], parameter).Visit(tagPredicate.Body)!;
            var combined = Expression.Lambda<Func<Certificate, bool>>(
                Expression.OrElse(fieldPredicate.Body, tagBody), parameter);

            return query.Where(combined);
        }

        private sealed class ParameterRebinder : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;

            public ParameterRebinder(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _from ? _to : base.VisitParameter(node);
        }
    }
}
