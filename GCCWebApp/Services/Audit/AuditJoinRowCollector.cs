using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GCCWebApp.Services.Audit
{
    /// <summary>
    /// DEF-337 (completion): turns many-to-many associations into restorable audit rows.
    ///
    /// The original DEF-337 fix captured the FK children of a delete (rows with a real
    /// CertificateId column). It did not capture the JOIN TABLES, because those are EF skip
    /// navigations backed by shadow join entities: there is no CLR type to query with
    /// <c>_context.Set&lt;T&gt;()</c> and nothing to serialize. They cascade away just the same.
    ///
    /// For a certificate that meant three association sets vanished with no record:
    /// CertificateTags, CertificatePartners and CertificateCertificationTypeSubcategories.
    /// The partner one matters most — restoring a certificate without its partner links
    /// silently changes who can see it (see the partner-isolation work behind DEF-103/198).
    ///
    /// This collector reads the join table and both column names from the EF model rather than
    /// hard-coding them, so the audit trail follows the mapping if it is ever changed.
    /// </summary>
    public static class AuditJoinRowCollector
    {
        /// <summary>
        /// Emits one <see cref="AuditChildSnapshot"/> per association held by a skip navigation.
        ///
        /// Call with the navigation already loaded (Include or explicit load) and BEFORE the
        /// parent is removed. Never throws — a failure to resolve mapping metadata degrades to
        /// link rows with null table/column names rather than losing the association entirely,
        /// and any other failure yields no rows rather than blocking the delete.
        /// </summary>
        /// <param name="context">Context whose model describes the relationship.</param>
        /// <param name="leftClrType">Owning entity type, e.g. typeof(Certificate).</param>
        /// <param name="navigationName">Skip navigation name on the owning type, e.g. "Partners".</param>
        /// <param name="leftId">Primary key of the owning entity being deleted.</param>
        /// <param name="rightEntities">The associated entities currently loaded on that navigation.</param>
        /// <param name="rightIdSelector">Extracts the related entity's key.</param>
        /// <param name="rightNameSelector">Optional display name for the related entity.</param>
        public static IReadOnlyList<AuditChildSnapshot> Collect<TRight>(
            DbContext context,
            Type leftClrType,
            string navigationName,
            string leftId,
            IEnumerable<TRight>? rightEntities,
            Func<TRight, string> rightIdSelector,
            Func<TRight, string?>? rightNameSelector = null)
        {
            var snapshots = new List<AuditChildSnapshot>();
            if (rightEntities == null)
                return snapshots;

            string? joinTable = null;
            string? leftColumn = null;
            string? rightColumn = null;
            var leftEntityName = leftClrType.Name;
            var rightEntityName = typeof(TRight).Name;

            try
            {
                var skipNav = context.Model.FindEntityType(leftClrType)?.FindSkipNavigation(navigationName);
                if (skipNav != null)
                {
                    joinTable = skipNav.JoinEntityType?.GetTableName();

                    // skipNav.ForeignKey is the join entity's FK back to the DECLARING (left) side.
                    leftColumn = skipNav.ForeignKey?.Properties.FirstOrDefault()?.GetColumnName();

                    // The inverse navigation is null for relationships configured with a bare
                    // .WithMany() (CertificationTypeSubcategories, CertificateProductId.Partners),
                    // so the right-hand FK is found as "the join entity's other foreign key"
                    // rather than via skipNav.Inverse.
                    var rightFk = skipNav.JoinEntityType?
                        .GetForeignKeys()
                        .FirstOrDefault(fk => fk != skipNav.ForeignKey);
                    rightColumn = rightFk?.Properties.FirstOrDefault()?.GetColumnName();
                }
            }
            catch
            {
                // Metadata resolution is a convenience for whoever writes the restore script.
                // Losing it must not cost us the association itself, so fall through with nulls.
            }

            foreach (var right in rightEntities)
            {
                if (right == null)
                    continue;

                var rightId = rightIdSelector(right);
                snapshots.Add(new AuditChildSnapshot
                {
                    // Named for the join table so the audit row reads as the link it is,
                    // not as the related entity (which is NOT being deleted).
                    EntityType = joinTable ?? $"{leftEntityName}{rightEntityName}",
                    EntityId = $"{leftId}:{rightId}",
                    EntityName = rightNameSelector?.Invoke(right),
                    Entity = new AuditLinkRow
                    {
                        JoinTable = joinTable,
                        LeftEntity = leftEntityName,
                        LeftColumn = leftColumn,
                        LeftId = leftId,
                        RightEntity = rightEntityName,
                        RightColumn = rightColumn,
                        RightId = rightId,
                        RightName = rightNameSelector?.Invoke(right)
                    }
                });
            }

            return snapshots;
        }
    }
}
