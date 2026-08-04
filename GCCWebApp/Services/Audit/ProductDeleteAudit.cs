using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCCWebApp.Services.Audit
{
    /// <summary>
    /// DEF-337 (completion): captures everything a product hard-delete destroys.
    ///
    /// This is the case that exposed DEF-337 in the first place — products 100/101 were deleted
    /// from the admin UI on 2026-08-04 and restored by hand from the audit snapshot. The restore
    /// worked only because those two products happened to have no identifiers and no tags. The
    /// audit captured the product row and nothing else; had they carried ASINs, those would have
    /// been gone with no record anywhere.
    ///
    /// `ProductIdentifiers.ProductId` and the product/tag join are both ON DELETE CASCADE, so
    /// SQL Server removes them without application code ever seeing them, and the product's own
    /// snapshot cannot stand in — entity serialization strips navigation collections by design.
    ///
    /// Shared by the two product delete paths (admin UI and the v1 API) so they cannot drift.
    /// </summary>
    public static class ProductDeleteAudit
    {
        /// <summary>
        /// Loads the cascade children of a product. Call BEFORE removing it.
        /// </summary>
        public static async Task<IReadOnlyList<AuditChildSnapshot>> CollectChildrenAsync(
            ApplicationDbContext context, int productId)
        {
            var children = new List<AuditChildSnapshot>();

            var identifiers = await context.Set<ProductIdentifier>()
                .AsNoTracking()
                .Where(pi => pi.ProductId == productId)
                .ToListAsync();

            children.AddRange(identifiers.Select(pi => new AuditChildSnapshot
            {
                EntityType = nameof(ProductIdentifier),
                EntityId = pi.Id.ToString(),
                EntityName = $"{pi.IdentifierType}: {pi.IdentifierValue}",
                Entity = pi
            }));

            // Tags are NOT deleted with the product — only the association is. Captured as link
            // rows so a restore re-creates the join rows rather than re-creating tags.
            var product = await context.Products
                .AsNoTracking()
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product != null)
            {
                children.AddRange(AuditJoinRowCollector.Collect(
                    context, typeof(Product), nameof(Product.Tags), productId.ToString(),
                    product.Tags, t => t.Id.ToString(), t => t.Name));
            }

            return children;
        }
    }
}
