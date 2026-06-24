using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GCCWebApp.Services
{
    /// <summary>
    /// DEF-286 (QC #198): single source of truth for where uploaded certificate files
    /// physically live, and how a logical DB path ("uploads/&lt;subdir&gt;/&lt;file&gt;") maps to a
    /// physical file.
    ///
    /// History: DEF-208 moved uploads out of wwwroot to ContentRootPath/App_Data/uploads
    /// (so static-file middleware can't serve them). DEF-262/264/279 then chased the same
    /// "this resolver was updated, the adjacent one wasn't" bug across five+ call sites.
    /// Centralising here kills that whole bug class — every reader/writer goes through one
    /// resolver — and lets the upload root be configured to a path OUTSIDE the deployment
    /// folder so a Plesk publish can no longer wipe the store (the 2026-06-15 incident).
    ///
    /// Config: <c>FileStorage:StoragePath</c> = the uploads root (the folder that directly
    /// contains <c>certificates/</c> and <c>images/</c>). Absolute path → used as-is; relative
    /// → resolved under ContentRootPath. When empty/unset, defaults to the DEF-208 home
    /// <c>ContentRootPath/App_Data/uploads</c>, so existing environments behave exactly as before.
    ///
    /// Reads fall back, in order: configured root → App_Data/uploads (the pre-config default
    /// home, only tried if the configured root differs) → wwwroot/uploads (pre-DEF-208 legacy).
    /// </summary>
    public interface IUploadPathResolver
    {
        /// <summary>Folder that directly contains <c>certificates/</c> and <c>images/</c>.</summary>
        string UploadsRoot { get; }

        /// <summary>Physical directory new certificate PDFs are written to.</summary>
        string CertificatesDir { get; }

        /// <summary>Physical directory generated thumbnails/images are written to.</summary>
        string ImagesDir { get; }

        /// <summary>
        /// Resolve a logical "uploads/&lt;subdir&gt;/&lt;file&gt;" path to a physical path.
        /// Returns the first existing candidate; if none exist, returns the canonical
        /// configured-root candidate so the caller's own File.Exists handles absence.
        /// </summary>
        string ResolvePhysical(string logicalPath);

        /// <summary>
        /// Like <see cref="ResolvePhysical"/> but reports whether a file actually exists.
        /// <paramref name="physicalPath"/> is the resolved path (canonical candidate on a miss,
        /// empty only when the logical path is malformed / a traversal attempt).
        /// </summary>
        bool TryResolveExisting(string logicalPath, out string physicalPath);
    }

    public class UploadPathResolver : IUploadPathResolver
    {
        private readonly ILogger<UploadPathResolver> _logger;
        private readonly string _appDataUploads; // DEF-208 default home
        private readonly string _wwwrootUploads;  // pre-DEF-208 legacy

        public string UploadsRoot { get; }
        public string CertificatesDir { get; }
        public string ImagesDir { get; }

        public UploadPathResolver(IWebHostEnvironment env, IConfiguration config, ILogger<UploadPathResolver> logger)
        {
            _logger = logger;

            // Fail loudly: a missing ContentRootPath must never silently land uploads at the
            // application root (carried over from the DEF-208 FileUploadService guard).
            if (string.IsNullOrEmpty(env.ContentRootPath))
            {
                _logger.LogCritical("ContentRootPath is null or empty at UploadPathResolver init.");
                throw new InvalidOperationException(
                    "ContentRootPath is null or empty. Aborting UploadPathResolver init to " +
                    "prevent uploads landing at the application root.");
            }

            _appDataUploads = Path.Combine(env.ContentRootPath, "App_Data", "uploads");
            _wwwrootUploads = string.IsNullOrEmpty(env.WebRootPath)
                ? string.Empty
                : Path.Combine(env.WebRootPath, "uploads");

            var configured = config["FileStorage:StoragePath"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                UploadsRoot = Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(env.ContentRootPath, configured);
                _logger.LogInformation(
                    "Upload storage root configured via FileStorage:StoragePath → {UploadsRoot}", UploadsRoot);
            }
            else
            {
                UploadsRoot = _appDataUploads;
            }

            CertificatesDir = Path.Combine(UploadsRoot, "certificates");
            ImagesDir = Path.Combine(UploadsRoot, "images");

            try
            {
                Directory.CreateDirectory(CertificatesDir);
                Directory.CreateDirectory(ImagesDir);
            }
            catch (Exception ex)
            {
                // Don't crash startup — log loudly; the directory may be created lazily on first
                // write, and a misconfigured StoragePath should surface here, not silently.
                _logger.LogError(ex, "Failed to ensure upload directories under {UploadsRoot}", UploadsRoot);
            }
        }

        /// <summary>
        /// Normalize a logical path to a relative path WITHIN the uploads root: strip ~/leading
        /// separators, normalize slashes, drop a leading "uploads/" segment (the uploads root IS
        /// the uploads folder), and reject traversal. Returns null when malformed.
        /// </summary>
        private static string? ToRelativeWithinUploads(string logicalPath)
        {
            if (string.IsNullOrEmpty(logicalPath)) return null;

            var clean = logicalPath.TrimStart('~').TrimStart('/', '\\').Replace('\\', '/');
            if (clean.Contains("..", StringComparison.Ordinal)) return null;

            if (clean.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring("uploads/".Length);

            if (clean.Length == 0) return null;
            return clean.Replace('/', Path.DirectorySeparatorChar);
        }

        public bool TryResolveExisting(string logicalPath, out string physicalPath)
        {
            physicalPath = string.Empty;

            var rel = ToRelativeWithinUploads(logicalPath);
            if (rel == null)
            {
                _logger.LogWarning("Rejected malformed/suspicious upload path: {Path}", logicalPath);
                return false;
            }

            // 1) Configured (or default) uploads root — the canonical home.
            var primary = Path.Combine(UploadsRoot, rel);
            physicalPath = primary;
            if (File.Exists(primary)) return true;

            // 2) App_Data home, only if a config moved the root elsewhere.
            if (!string.Equals(UploadsRoot, _appDataUploads, StringComparison.OrdinalIgnoreCase))
            {
                var appData = Path.Combine(_appDataUploads, rel);
                if (File.Exists(appData)) { physicalPath = appData; return true; }
            }

            // 3) Pre-DEF-208 wwwroot legacy location.
            if (!string.IsNullOrEmpty(_wwwrootUploads))
            {
                var legacy = Path.Combine(_wwwrootUploads, rel);
                if (File.Exists(legacy)) { physicalPath = legacy; return true; }
            }

            return false;
        }

        public string ResolvePhysical(string logicalPath)
        {
            if (TryResolveExisting(logicalPath, out var physical) || !string.IsNullOrEmpty(physical))
                return physical;

            // Malformed path: return a guaranteed-nonexistent candidate so callers' File.Exists → false.
            return Path.Combine(UploadsRoot, "__unresolved__");
        }
    }
}
