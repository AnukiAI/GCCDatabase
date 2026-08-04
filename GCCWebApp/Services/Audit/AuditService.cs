using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        // Resolved at most once per scoped instance (i.e. per request) — see ResolveSystemUser.
        private bool _systemUserResolved;
        private (int UserId, string Username)? _systemUser;

        public AuditService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _configuration = configuration;
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
                // DEF-337 (completion): resolve the acting user the same way the delete path
                // does. Previously an unresolvable userId (0) was written straight through and
                // blew up on FK_AuditHistory_Users_ChangedBy inside SaveChanges, where the catch
                // below swallowed it — the audit row was lost with only a generic error logged.
                // Several newly-audited callers cannot supply a userId, so refuse explicitly.
                var (resolvedUserId, resolvedUsername) = ResolveActingUser(userId, username);
                if (resolvedUserId <= 0)
                {
                    _logger.LogError(
                        "AUDIT GAP: {Action} of {EntityType} {EntityId} was NOT audited — no " +
                        "resolvable user (userId={UserId}).", action, typeof(T).Name, entityId, userId);
                    return 0;
                }

                userId = resolvedUserId;
                username = resolvedUsername;

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
        /// DEF-337: DELETE audit for an entity plus every row SQL Server will cascade away.
        /// See IAuditService for the rationale. Parent + children are written in ONE batch.
        /// </summary>
        public async Task<int> LogEntityDeleteWithChildrenAsync<T>(
            string entityId,
            string? entityName,
            T entity,
            IReadOnlyList<AuditChildSnapshot> children,
            int userId,
            string username,
            string? reason = null,
            string? changeSource = null
        ) where T : class
        {
            try
            {
                var (resolvedUserId, resolvedUsername) = ResolveActingUser(userId, username);
                if (resolvedUserId <= 0)
                {
                    // ChangedBy is NOT NULL with FK_AuditHistory_Users_ChangedBy. Writing an
                    // unresolvable id throws inside SaveChanges and the catch below swallows it,
                    // which is exactly how DEF-187's audit rows were lost silently. Refuse
                    // loudly instead of writing a row that cannot persist.
                    _logger.LogError(
                        "AUDIT GAP: delete of {EntityType} {EntityId} was NOT audited — no resolvable " +
                        "user (userId={UserId}). The record and {ChildCount} cascade child row(s) are " +
                        "being destroyed with no audit trail.",
                        typeof(T).Name, entityId, userId, children?.Count ?? 0);
                    return 0;
                }

                var parentType = typeof(T).Name;
                var records = new List<AuditHistory>
                {
                    CreateAuditRecord(
                        parentType, entityId, entityName, AuditAction.Delete,
                        null, SerializeEntity(entity), null,
                        resolvedUserId, resolvedUsername, reason, changeSource)
                };

                if (children != null)
                {
                    foreach (var child in children)
                    {
                        records.Add(CreateAuditRecord(
                            child.EntityType,
                            child.EntityId,
                            child.EntityName,
                            AuditAction.Delete,
                            null,
                            // Runtime-typed: serializing as `object` would emit "{}".
                            SerializeEntityByRuntimeType(child.Entity),
                            null,
                            resolvedUserId,
                            resolvedUsername,
                            // Tag the parentage so a restore knows what these belonged to.
                            $"Cascade delete with {parentType} {entityId}" +
                                (string.IsNullOrWhiteSpace(reason) ? "" : $" — {reason}"),
                            changeSource));
                    }
                }

                await _context.AuditHistory.AddRangeAsync(records);
                await _context.SaveChangesAsync();
                return records.Count;
            }
            catch (Exception ex)
            {
                // Deliberately not rethrown (audit must not break business operations), but this
                // is logged at Error, not swallowed silently — a missing delete audit is data loss.
                _logger.LogError(ex,
                    "AUDIT GAP: failed to log delete of {EntityType} {EntityId} with {ChildCount} children",
                    typeof(T).Name, entityId, children?.Count ?? 0);
                return 0;
            }
        }

        /// <summary>
        /// DEF-337: falls back to the signed-in user's claims when a caller cannot supply the
        /// acting user. Mirrors the claim order established by DEF-187
        /// (NameIdentifier -> sub -> UserId); an explicit non-zero userId always wins.
        /// Returns userId 0 when nothing can be resolved, which callers must treat as "do not write".
        /// </summary>
        private (int UserId, string Username) ResolveActingUser(int userId, string? username)
        {
            if (userId > 0)
                return (userId, string.IsNullOrWhiteSpace(username) ? "unknown" : username);

            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated == true)
            {
                var claim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                            ?? principal.FindFirst("sub")
                            ?? principal.FindFirst("UserId");

                if (claim != null && int.TryParse(claim.Value, out var claimUserId) && claimUserId > 0)
                {
                    var name = username;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                               ?? principal.Identity.Name
                               ?? "unknown";
                    }
                    return (claimUserId, name);
                }
            }

            // DEF-337 (completion): API-key callers have no user behind them. The v1 API
            // authenticates with an ApiKey/PartnerApiKey, neither of which carries a Users row,
            // so nothing above can resolve — yet those endpoints delete certificates and
            // products. AuditHistory.ChangedBy is NOT NULL with an FK to Users, so the row
            // physically cannot be written without SOME user id.
            //
            // Rather than make the column nullable (schema change, and it would weaken every
            // other audit row) or hard-code an id, fall back to a designated service account
            // named in configuration. Unset = unchanged behaviour: refuse and log the gap.
            var systemUser = ResolveSystemUser();
            if (systemUser.HasValue)
                return systemUser.Value;

            return (0, username ?? "unknown");
        }

        /// <summary>
        /// DEF-337 (completion): looks up the configured audit service account
        /// (<c>Audit:SystemUserEmail</c>) so machine-initiated changes can be attributed.
        /// Cached per scoped instance so a bulk operation does not re-query per row.
        /// Returns null when unconfigured or when the configured account does not exist.
        /// </summary>
        private (int UserId, string Username)? ResolveSystemUser()
        {
            if (_systemUserResolved)
                return _systemUser;

            _systemUserResolved = true;

            try
            {
                var email = _configuration["Audit:SystemUserEmail"];
                if (string.IsNullOrWhiteSpace(email))
                    return _systemUser = null;

                var user = _context.Users
                    .AsNoTracking()
                    .Where(u => u.Email == email)
                    .Select(u => new { u.Id, u.Email })
                    .FirstOrDefault();

                if (user == null)
                {
                    _logger.LogWarning(
                        "Audit:SystemUserEmail is set to '{Email}' but no such user exists — " +
                        "machine-initiated changes will not be audited.", email);
                    return _systemUser = null;
                }

                return _systemUser = (user.Id, user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve the configured audit system user");
                return _systemUser = null;
            }
        }

        /// <summary>
        /// DEF-337: serializes by RUNTIME type. JsonSerializer.Serialize(obj, options) with a
        /// static type of object emits "{}" — it reflects over System.Object, not the instance.
        /// Cascade children arrive as object (several distinct types per delete), so they must
        /// go through this rather than SerializeEntity&lt;T&gt;.
        /// </summary>
        private string? SerializeEntityByRuntimeType(object? entity)
        {
            if (entity == null)
                return null;

            try
            {
                return JsonSerializer.Serialize(entity, entity.GetType(), EntitySerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize cascade child {EntityType}", entity.GetType().Name);
                return $"[Serialization failed: {entity.GetType().Name}]";
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
                // DEF-337 (completion): same acting-user resolution as the other write paths.
                var (resolvedUserId, resolvedUsername) = ResolveActingUser(userId, username);
                if (resolvedUserId <= 0)
                {
                    _logger.LogError(
                        "AUDIT GAP: {Action} event for {EntityType} {EntityId} was NOT audited — " +
                        "no resolvable user (userId={UserId}).", action, entityType, entityId, userId);
                    return;
                }

                var record = CreateAuditRecord(
                    entityType,
                    entityId,
                    entityName,
                    action,
                    null,
                    null,
                    details,
                    resolvedUserId,
                    resolvedUsername,
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
        // DEF-337 (completion): "ApiKey", "Key" and "KeyHash" added when API-key deletes became
        // audited. PartnerApiKey.ApiKey holds the key in plaintext, and an audit blob is a
        // long-lived, widely-readable copy — exactly the wrong place for live credentials. The
        // consequence is intentional: a deleted key cannot be reconstructed from the audit
        // trail. A revoked key should never be restored anyway; issue a new one.
        private static readonly string[] AuditSkipFields =
            { "UpdatedAt", "UpdatedBy", "UpdatedByUser", "CreatedByUser", "User", "PasswordHash",
              "ApiKey", "Key", "KeyHash" };

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
