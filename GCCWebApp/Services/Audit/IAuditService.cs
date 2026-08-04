using GCCWebApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCCWebApp.Services.Audit
{
    /// <summary>
    /// Service for tracking and querying audit history across all entities
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Logs a change to any entity with automatic field-level change detection
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entityId">Primary key of entity</param>
        /// <param name="entityName">Display name for entity (e.g., product name, company name)</param>
        /// <param name="action">CREATE, UPDATE, DELETE, etc.</param>
        /// <param name="oldEntity">Entity before change (null for CREATE)</param>
        /// <param name="newEntity">Entity after change (null for DELETE)</param>
        /// <param name="userId">User who made the change</param>
        /// <param name="username">Username of user</param>
        /// <param name="reason">Optional reason for change</param>
        /// <param name="changeSource">Source of change (Web UI, API, Import, etc.)</param>
        /// <param name="importBatchId">Optional import batch correlation id (CR-023) — stamped on
        /// every audit row written for one import apply so the batch can later be undone.</param>
        /// <returns>Number of audit records created</returns>
        Task<int> LogEntityChangeAsync<T>(
            string entityId,
            string? entityName,
            string action,
            T? oldEntity,
            T? newEntity,
            int userId,
            string username,
            string? reason = null,
            string? changeSource = null,
            Guid? importBatchId = null
        ) where T : class;

        /// <summary>
        /// DEF-337: logs a DELETE for an entity together with the rows that SQL Server will
        /// destroy via ON DELETE CASCADE, in a single batched write.
        ///
        /// Why this exists: cascade children (e.g. CertificateProductIds, CertificatePartners,
        /// ProductIdentifiers) are removed at the database layer, so no application code ever
        /// sees them go. The parent's own snapshot cannot stand in for them either — entity
        /// serialization deliberately strips navigation collections. Before this method,
        /// deleting a certificate destroyed seven child tables with no record of what they held.
        ///
        /// Each child is written as its own DELETE row (EntityType = the child's type), tagged
        /// via <paramref name="reason"/> with the parent it cascaded from, so children are
        /// individually inspectable and restorable. Everything is persisted in ONE SaveChanges
        /// rather than one per child.
        ///
        /// Call this BEFORE removing the parent, while the children are still loaded.
        /// </summary>
        /// <param name="children">Cascade children to capture. Pass an empty list if none.</param>
        /// <returns>Number of audit records created (1 parent + one per child)</returns>
        Task<int> LogEntityDeleteWithChildrenAsync<T>(
            string entityId,
            string? entityName,
            T entity,
            IReadOnlyList<AuditChildSnapshot> children,
            int userId,
            string username,
            string? reason = null,
            string? changeSource = null
        ) where T : class;

        /// <summary>
        /// Logs a simple audit event without field-level tracking
        /// Useful for events like LOGIN, LOGOUT, etc.
        /// </summary>
        Task LogAuditEventAsync(
            string entityType,
            string entityId,
            string? entityName,
            string action,
            int userId,
            string username,
            string? details = null,
            string? reason = null,
            string? changeSource = null
        );

        /// <summary>
        /// Gets audit history for a specific entity
        /// </summary>
        Task<List<AuditHistory>> GetEntityHistoryAsync(
            string entityType,
            string entityId,
            int? limit = 100
        );

        /// <summary>
        /// Gets audit history for a specific user's actions
        /// </summary>
        Task<List<AuditHistory>> GetUserAuditHistoryAsync(
            int userId,
            int? limit = 100
        );

        /// <summary>
        /// Gets recent audit activity across all entities
        /// </summary>
        Task<List<AuditHistory>> GetRecentAuditHistoryAsync(
            int? limit = 50,
            string? entityType = null,
            string? action = null,
            DateTime? since = null
        );

        /// <summary>
        /// Searches audit history with advanced filters
        /// </summary>
        Task<(List<AuditHistory> Results, int TotalCount)> SearchAuditHistoryAsync(
            string? entityType = null,
            string? entityId = null,
            string? action = null,
            int? userId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50
        );

        /// <summary>
        /// Gets aggregated statistics about audit activity
        /// </summary>
        Task<AuditStatistics> GetAuditStatisticsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null
        );
    }

    /// <summary>
    /// DEF-337: one cascade-deleted child row, captured before its parent is removed.
    /// <see cref="Entity"/> is typed as object deliberately — a single delete cascades to
    /// several different child types, so they cannot share one generic parameter. The audit
    /// service serializes it by RUNTIME type; serializing it as object would emit "{}".
    /// </summary>
    public sealed class AuditChildSnapshot
    {
        /// <summary>Child entity type name, e.g. "CertificateProductId".</summary>
        public required string EntityType { get; init; }

        /// <summary>Child primary key, as a string.</summary>
        public required string EntityId { get; init; }

        /// <summary>Optional display name for the child.</summary>
        public string? EntityName { get; init; }

        /// <summary>The child entity instance to snapshot.</summary>
        public required object Entity { get; init; }
    }

    /// <summary>
    /// DEF-337 (completion): one row of a many-to-many JOIN TABLE, captured before a cascade
    /// destroys it.
    ///
    /// Why this is not just an <see cref="AuditChildSnapshot"/> of the related entity: the
    /// related entity is NOT being deleted. Deleting a certificate does not delete the Tag or
    /// the Partner — it deletes the *association* between them. Snapshotting the Tag would
    /// record the wrong thing (and imply the tag was destroyed); what has to be restorable is
    /// the link row itself.
    ///
    /// These join tables have no CLR entity type (EF skip navigations with shadow join
    /// entities), so they cannot be queried or serialized like the FK children can. The join
    /// table and column names are read from the EF model at capture time rather than
    /// hard-coded, so a restore stays correct if the mapping ever changes.
    ///
    /// All properties are scalars deliberately — the audit serializer strips IEnumerable
    /// properties, so a snapshot holding a collection of ids would serialize to nothing.
    /// One AuditLinkRow is written per association.
    /// </summary>
    public sealed class AuditLinkRow
    {
        /// <summary>Physical join table, e.g. "CertificatePartners". Null if unresolvable.</summary>
        public string? JoinTable { get; init; }

        /// <summary>Left-hand entity type, e.g. "Certificate".</summary>
        public required string LeftEntity { get; init; }

        /// <summary>Join-table column holding the left key, e.g. "CertificatesId".</summary>
        public string? LeftColumn { get; init; }

        /// <summary>Left key value.</summary>
        public required string LeftId { get; init; }

        /// <summary>Right-hand entity type, e.g. "Partner".</summary>
        public required string RightEntity { get; init; }

        /// <summary>Join-table column holding the right key, e.g. "PartnersId".</summary>
        public string? RightColumn { get; init; }

        /// <summary>Right key value.</summary>
        public required string RightId { get; init; }

        /// <summary>Display name of the right-hand entity, for human-readable audit review.</summary>
        public string? RightName { get; init; }
    }

    /// <summary>
    /// Statistics about audit activity
    /// </summary>
    public class AuditStatistics
    {
        public int TotalAuditRecords { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueEntities { get; set; }
        public Dictionary<string, int> ActionCounts { get; set; } = new();
        public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
        public List<UserActivitySummary> TopUsers { get; set; } = new();
    }

    /// <summary>
    /// Summary of a user's audit activity
    /// </summary>
    public class UserActivitySummary
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int ActionCount { get; set; }
        public DateTime? LastActivity { get; set; }
    }
}
