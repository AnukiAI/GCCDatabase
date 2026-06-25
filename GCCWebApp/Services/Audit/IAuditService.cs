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
