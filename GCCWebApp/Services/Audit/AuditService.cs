using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace GCCWebApp.Services.Audit
{
    /// <summary>
    /// Service for comprehensive audit logging with automatic field-level change detection
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Logs entity changes with automatic field-level detection
        /// </summary>
        public async Task<int> LogEntityChangeAsync<T>(
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
        ) where T : class
        {
            try
            {
                var entityType = typeof(T).Name;
                var auditRecords = new List<AuditHistory>();

                // For CREATE, DELETE, or RESTORE, log single record with full entity data.
                // RESTORE (CR-023 import undo) behaves like CREATE: the restored/new state goes in
                // NewValue. Without this it would match no branch and write nothing.
                if (action == AuditAction.Create || action == AuditAction.Delete || action == AuditAction.Restore)
                {
                    var singleRecord = CreateAuditRecord(
                        entityType,
                        entityId,
                        entityName,
                        action,
                        null, // No specific field
                        action == AuditAction.Create || action == AuditAction.Restore ? null : SerializeEntity(oldEntity),
                        action == AuditAction.Delete ? null : SerializeEntity(newEntity),
                        userId,
                        username,
                        reason,
                        changeSource,
                        importBatchId
                    );
                    auditRecords.Add(singleRecord);
                }
                // For UPDATE, detect field-level changes
                else if (action == AuditAction.Update)
                {
                    var changes = DetectChanges(oldEntity, newEntity);

                    if (changes.Count == 0)
                    {
                        // No actual changes detected, log a single record indicating attempted update
                        _logger.LogInformation("No changes detected for {EntityType} {EntityId} update", entityType, entityId);
                        return 0;
                    }

                    foreach (var change in changes)
                    {
                        var record = CreateAuditRecord(
                            entityType,
                            entityId,
                            entityName,
                            action,
                            change.FieldName,
                            change.OldValue,
                            change.NewValue,
                            userId,
                            username,
                            reason,
                            changeSource,
                            importBatchId
                        );
                        auditRecords.Add(record);
                    }
                }

                if (auditRecords.Count > 0)
                {
                    await _context.AuditHistory.AddRangeAsync(auditRecords);
                    await _context.SaveChangesAsync();
                }

                return auditRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit for {EntityType} {EntityId}", typeof(T).Name, entityId);
                // Don't throw - audit failures shouldn't break business operations
                return 0;
            }
        }

        /// <summary>
        /// Logs a simple audit event without field-level tracking
        /// </summary>
        public async Task LogAuditEventAsync(
            string entityType,
            string entityId,
            string? entityName,
            string action,
            int userId,
            string username,
            string? details = null,
            string? reason = null,
            string? changeSource = null
        )
        {
            try
            {
                var record = CreateAuditRecord(
                    entityType,
                    entityId,
                    entityName,
                    action,
                    null,
                    null,
                    details,
                    userId,
                    username,
                    reason,
                    changeSource
                );

                await _context.AuditHistory.AddAsync(record);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit event for {EntityType} {EntityId}", entityType, entityId);
            }
        }

        /// <summary>
        /// Gets audit history for a specific entity
        /// </summary>
        public async Task<List<AuditHistory>> GetEntityHistoryAsync(
            string entityType,
            string entityId,
            int? limit = 100
        )
        {
            var query = _context.AuditHistory
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.ChangedAt);

            if (limit.HasValue && limit.Value > 0)
            {
                query = (IOrderedQueryable<AuditHistory>)query.Take(limit.Value);
            }

            return await query.Include(a => a.User).ToListAsync();
        }

        /// <summary>
        /// Gets audit history for a specific user's actions
        /// </summary>
        public async Task<List<AuditHistory>> GetUserAuditHistoryAsync(
            int userId,
            int? limit = 100
        )
        {
            var query = _context.AuditHistory
                .Where(a => a.ChangedBy == userId)
                .OrderByDescending(a => a.ChangedAt);

            if (limit.HasValue && limit.Value > 0)
            {
                query = (IOrderedQueryable<AuditHistory>)query.Take(limit.Value);
            }

            return await query.Include(a => a.User).ToListAsync();
        }

        /// <summary>
        /// Gets recent audit activity
        /// </summary>
        public async Task<List<AuditHistory>> GetRecentAuditHistoryAsync(
            int? limit = 50,
            string? entityType = null,
            string? action = null,
            DateTime? since = null
        )
        {
            var query = _context.AuditHistory.AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);

            if (since.HasValue)
                query = query.Where(a => a.ChangedAt >= since.Value);

            var orderedQuery = query.OrderByDescending(a => a.ChangedAt);

            if (limit.HasValue && limit.Value > 0)
            {
                orderedQuery = (IOrderedQueryable<AuditHistory>)orderedQuery.Take(limit.Value);
            }

            return await orderedQuery.Include(a => a.User).ToListAsync();
        }

        /// <summary>
        /// Searches audit history with pagination
        /// </summary>
        public async Task<(List<AuditHistory> Results, int TotalCount)> SearchAuditHistoryAsync(
            string? entityType = null,
            string? entityId = null,
            string? action = null,
            int? userId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50
        )
        {
            var query = _context.AuditHistory.AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (!string.IsNullOrEmpty(entityId))
                query = query.Where(a => a.EntityId == entityId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);

            if (userId.HasValue)
                query = query.Where(a => a.ChangedBy == userId.Value);

            if (startDate.HasValue)
                query = query.Where(a => a.ChangedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.ChangedAt <= endDate.Value);

            var totalCount = await query.CountAsync();

            var results = await query
                .OrderByDescending(a => a.ChangedAt)
                .Include(a => a.User)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (results, totalCount);
        }

        /// <summary>
        /// Gets audit statistics
        /// </summary>
        public async Task<AuditStatistics> GetAuditStatisticsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null
        )
        {
            var query = _context.AuditHistory.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.ChangedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.ChangedAt <= endDate.Value);

            var stats = new AuditStatistics
            {
                TotalAuditRecords = await query.CountAsync(),
                UniqueUsers = await query.Select(a => a.ChangedBy).Distinct().CountAsync(),
                UniqueEntities = await query.Select(a => new { a.EntityType, a.EntityId }).Distinct().CountAsync(),
                ActionCounts = await query.GroupBy(a => a.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Action, x => x.Count),
                EntityTypeCounts = await query.GroupBy(a => a.EntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.EntityType, x => x.Count),
                TopUsers = await query.GroupBy(a => new { a.ChangedBy, a.ChangedByUsername })
                    .Select(g => new UserActivitySummary
                    {
                        UserId = g.Key.ChangedBy,
                        Username = g.Key.ChangedByUsername,
                        ActionCount = g.Count(),
                        LastActivity = g.Max(a => a.ChangedAt)
                    })
                    .OrderByDescending(u => u.ActionCount)
                    .Take(10)
                    .ToListAsync()
            };

            return stats;
        }

        #region Private Helper Methods

        /// <summary>
        /// Creates an audit record with HTTP context information
        /// </summary>
        private AuditHistory CreateAuditRecord(
            string entityType,
            string entityId,
            string? entityName,
            string action,
            string? fieldName,
            string? oldValue,
            string? newValue,
            int userId,
            string username,
            string? reason,
            string? changeSource,
            Guid? importBatchId = null
        )
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var sessionId = httpContext?.Session?.Id;
            var ipAddress = GetClientIpAddress();
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

            return new AuditHistory
            {
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Action = action,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedBy = userId,
                ChangedByUsername = username,
                ChangedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ChangeSource = changeSource ?? AuditChangeSource.WebUI,
                Reason = reason,
                SessionId = sessionId,
                ImportBatchId = importBatchId
            };
        }

        /// <summary>
        /// Detects field-level changes between two entity instances
        /// </summary>
        private List<FieldChange> DetectChanges<T>(T? oldEntity, T? newEntity) where T : class
        {
            var changes = new List<FieldChange>();

            if (oldEntity == null || newEntity == null)
                return changes;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // Skip navigation properties, collections, and internal tracking fields
                if (ShouldSkipProperty(prop))
                    continue;

                var oldValue = prop.GetValue(oldEntity);
                var newValue = prop.GetValue(newEntity);

                // Compare values
                if (!ValuesAreEqual(oldValue, newValue))
                {
                    changes.Add(new FieldChange
                    {
                        FieldName = prop.Name,
                        OldValue = SerializeValue(oldValue),
                        NewValue = SerializeValue(newValue)
                    });
                }
            }

            return changes;
        }

        // Shared skip list for both DetectChanges (field-level UPDATE diff) and SerializeEntity
        // (CREATE/DELETE full-entity JSON). Single source of truth so the two audit code paths
        // can't drift. PasswordHash is defense-in-depth here — the User model also marks it
        // [JsonIgnore] (DEF-121) — but keeping it in this list means any future credential
        // field added without [JsonIgnore] is still excluded from audit blobs.
        private static readonly string[] AuditSkipFields =
            { "UpdatedAt", "UpdatedBy", "UpdatedByUser", "CreatedByUser", "User", "PasswordHash" };

        /// <summary>
        /// Determines if a property should be skipped during change detection
        /// </summary>
        private bool ShouldSkipProperty(PropertyInfo prop)
        {
            // Skip collections and navigation properties
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                return true;

            if (AuditSkipFields.Contains(prop.Name))
                return true;

            return false;
        }

        /// <summary>
        /// Compares two values for equality
        /// </summary>
        private bool ValuesAreEqual(object? oldValue, object? newValue)
        {
            if (oldValue == null && newValue == null)
                return true;

            if (oldValue == null || newValue == null)
                return false;

            // Handle DateTime comparison with tolerance for milliseconds
            if (oldValue is DateTime oldDate && newValue is DateTime newDate)
            {
                return Math.Abs((oldDate - newDate).TotalSeconds) < 1;
            }

            return oldValue.Equals(newValue);
        }

        /// <summary>
        /// Serializes a value to string for storage
        /// </summary>
        private string? SerializeValue(object? value)
        {
            if (value == null)
                return null;

            if (value is string str)
                return str;

            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss");

            if (value.GetType().IsPrimitive || value is decimal)
                return value.ToString();

            // For complex objects, serialize to JSON
            try
            {
                return JsonSerializer.Serialize(value);
            }
            catch
            {
                return value.ToString();
            }
        }

        // DEF-187 (rolled-in audit-quality fix): static, cached JsonSerializerOptions for entity
        // serialization. Previously a fresh options object was allocated per call (cheap, but
        // each one rebuilt its TypeInfo cache from scratch). Caching across calls is the
        // documented best practice for System.Text.Json and avoids reflection cost on every
        // CREATE/DELETE audit write.
        private static readonly JsonSerializerOptions EntitySerializerOptions = BuildEntitySerializerOptions();

        private static JsonSerializerOptions BuildEntitySerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                // Break cycles in navigation properties. Without this, EF entities with
                // bidirectional nav (User <-> Roles, Certificate <-> Products, etc.) threw
                // inside JsonSerializer.Serialize and SerializeEntity returned the placeholder
                // string "[Serialization failed: ...]" — confirmed by query of dev AuditHistory:
                // every User and Certificate CREATE row had useless placeholder text instead
                // of the actual entity snapshot.
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };
            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    typeInfo =>
                    {
                        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
                        // Iterate in reverse so RemoveAt indices stay valid.
                        for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
                        {
                            var jp = typeInfo.Properties[i];
                            // 1. Drop fields in the shared audit skip list (PasswordHash, etc.).
                            if (AuditSkipFields.Contains(jp.Name, StringComparer.OrdinalIgnoreCase))
                            {
                                typeInfo.Properties.RemoveAt(i);
                                continue;
                            }
                            // 2. Drop navigation collections so audit blobs stay small and
                            // focused on the entity's own scalar fields. This mirrors
                            // DetectChanges' behavior — both paths agree on what counts as
                            // "the entity itself" vs. "related graph."
                            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(jp.PropertyType)
                                && jp.PropertyType != typeof(string))
                            {
                                typeInfo.Properties.RemoveAt(i);
                            }
                        }
                    }
                }
            };
            return options;
        }

        /// <summary>
        /// Serializes an entire entity to JSON
        /// </summary>
        private string? SerializeEntity<T>(T? entity) where T : class
        {
            if (entity == null)
                return null;

            try
            {
                return JsonSerializer.Serialize(entity, EntitySerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize entity {EntityType}", typeof(T).Name);
                return $"[Serialization failed: {typeof(T).Name}]";
            }
        }

        /// <summary>
        /// Gets client IP address from HTTP context
        /// </summary>
        private string? GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            // Check for forwarded IP (proxy scenarios)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                return ips[0].Trim();
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        #endregion

        /// <summary>
        /// Represents a field-level change
        /// </summary>
        private class FieldChange
        {
            public string FieldName { get; set; } = string.Empty;
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
        }
    }
}
