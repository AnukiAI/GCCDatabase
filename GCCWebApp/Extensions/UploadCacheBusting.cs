using System.Security.Cryptography;
using System.Text;

namespace GCCWebApp.Extensions
{
    /// <summary>
    /// DEF-310 (QC #191): cache-busting token for the certificate preview/document endpoints.
    ///
    /// /Certificate/ViewCertificateImage/{id} and /Certificate/ViewCertificateFile/{id} are
    /// stable per-certificate URLs — replacing the uploaded document on Edit writes a NEW file
    /// on disk and repoints the DB column, but the browser-facing URL does not change. With no
    /// Cache-Control header on those responses the browser applies heuristic freshness (roughly
    /// 10% of the document's age), so a replacement on a long-standing certificate is served
    /// from cache and the analyst sees the OLD document. On a freshly-created certificate the
    /// heuristic lifetime is seconds, so the same action appears to work — which is why the
    /// defect looked intermittent and would not reproduce on a test certificate.
    ///
    /// Appending this token to the URL changes the cache key whenever the stored file changes,
    /// which also defeats any intermediary/CDN cache that ignores request-time revalidation.
    /// It is defence in depth alongside the `private, no-cache, must-revalidate` header the two
    /// endpoints now emit.
    ///
    /// The token is a truncated hash rather than the filename itself: the stored filename is
    /// deliberately unguessable (DEF-193) and must not be leaked back out in a query string.
    /// </summary>
    public static class UploadCacheBusting
    {
        /// <summary>
        /// Returns a short, stable token derived from a stored upload path
        /// ("uploads/images/{file}.png"). Same path in → same token out, so the URL is stable
        /// until the underlying file is replaced. Returns an empty string for a missing path so
        /// callers can omit the query parameter entirely.
        /// </summary>
        public static string VersionToken(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return string.Empty;
            }

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(storedPath));
            return Convert.ToHexString(bytes, 0, 4).ToLowerInvariant(); // 8 hex chars
        }
    }
}
