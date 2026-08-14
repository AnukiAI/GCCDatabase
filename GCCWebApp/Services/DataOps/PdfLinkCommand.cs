using System.Globalization;
using System.Security.Cryptography;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Search;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Services.DataOps
{
    /// <summary>
    /// Migration plan §10 — attach the certificate PDFs pulled from the client's own system to
    /// the migrated <see cref="Certificate"/> rows.
    ///
    /// Run as an argv branch rather than a page or a script:
    ///   dotnet run -- link-pdfs --manifest C:\Users\swest\GCCPortalPull\manifest.csv --dry-run
    ///
    /// WHY C# AND NOT PYTHON/SQL (the decisive constraint): <c>CertificateImagePath</c> must be
    /// set in the same pass or the certificate renders a blank preview — indistinguishable from
    /// the by-design pre-save blank, so it would land as a UAT defect. Thumbnails come from
    /// <see cref="IFileUploadService.GeneratePdfThumbnailAsync"/> (Docnet + ImageSharp, two-pass
    /// aspect detection). Reimplementing that outside the app is how the two drift apart.
    ///
    /// This calls the app's own services and mirrors CertificateImportService.UploadPdfFromPathAsync
    /// exactly — same directory (<see cref="IUploadPathResolver.CertificatesDir"/>), same file name
    /// (<c>{Number}_{yyyyMMdd_HHmmss}.pdf</c>), same logical DB path. The <c>{Number}_</c> prefix is
    /// load-bearing in ops, not cosmetic: the recovery script globs <c>26-0174_*.pdf</c>.
    ///
    /// Safety properties, in the order they matter:
    ///   * Idempotent — a certificate that already has a CertificateFile is skipped (--force overrides).
    ///   * Verified — the source file's sha256 is checked against the manifest BEFORE the DB is touched.
    ///   * Reversible — every run writes an undo CSV; rollback is NULLing two columns and deleting
    ///     the copied files. No DB restore needed.
    ///   * Honest — ambiguous and unmatched rows are written to review CSVs rather than silently dropped.
    /// </summary>
    public static class PdfLinkCommand
    {
        public const string Verb = "link-pdfs";

        public static bool Matches(string[] args) =>
            args.Length > 0 && string.Equals(args[0], Verb, StringComparison.OrdinalIgnoreCase);

        // Deliberately NOT written to the audit table. This is a bulk migration attach, not a user
        // action; 5,000+ synthetic audit rows would bury the real history operators read. The undo
        // CSV plus the run log is the audit trail for this step.
        public static async Task<int> RunAsync(WebApplication app, string[] args)
        {
            var opts = Options.Parse(args);
            if (opts.Error != null)
            {
                Console.Error.WriteLine($"ERROR: {opts.Error}");
                Console.Error.WriteLine(Usage);
                return 2;
            }

            using var scope = app.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var paths = sp.GetRequiredService<IUploadPathResolver>();
            var uploads = sp.GetRequiredService<IFileUploadService>();
            var log = sp.GetRequiredService<ILogger<Program>>();

            var started = DateTime.UtcNow;
            var manifestDir = Path.GetDirectoryName(Path.GetFullPath(opts.ManifestPath))!;
            var outDir = opts.OutDir ?? Path.Combine(manifestDir, "_link-pdfs");
            Directory.CreateDirectory(outDir);
            var stamp = started.ToString("yyyyMMdd-HHmmss");

            Console.WriteLine(new string('=', 78));
            Console.WriteLine("link-pdfs — attach migrated certificate PDFs (plan §10)");
            Console.WriteLine(new string('=', 78));
            Console.WriteLine($"Manifest    : {opts.ManifestPath}");
            Console.WriteLine($"Files root  : {manifestDir}");
            Console.WriteLine($"Uploads dir : {paths.CertificatesDir}");
            Console.WriteLine($"Database    : {db.Database.GetDbConnection().Database}");
            Console.WriteLine($"Mode        : {(opts.DryRun ? "DRY RUN (no files copied, no DB writes)" : opts.Force ? "FORCE re-link" : "link (idempotent)")}");
            Console.WriteLine($"Reports     : {outDir}");
            Console.WriteLine();

            // ---------------------------------------------------------------- manifest
            List<ManifestRow> rows;
            try
            {
                rows = ReadManifest(opts.ManifestPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: could not read the manifest: {ex.Message}");
                return 1;
            }
            Console.WriteLine($"Manifest rows                : {rows.Count}");

            var usable = rows.Where(r =>
                !string.IsNullOrWhiteSpace(r.CertificateNumber) &&
                !string.IsNullOrWhiteSpace(r.LocalPath) &&
                !string.IsNullOrWhiteSpace(r.Sha256)).ToList();
            Console.WriteLine($"  with a downloaded file     : {usable.Count}");

            if (opts.Statuses.Count > 0)
            {
                var before = usable.Count;
                usable = usable.Where(r => opts.Statuses.Contains(r.CertStatus ?? "")).ToList();
                Console.WriteLine($"  after --status filter      : {usable.Count}  (was {before})");
            }

            // One certificate number can appear on several portal records (178 of them do — one
            // certificate covering several products). Our Certificate.Number is UNIQUE, so exactly
            // one file can be attached. Pick the lowest record_id for determinism and write the
            // alternates out — silently dropping them would look like data loss later.
            var grouped = usable
                .GroupBy(r => Key(r.CertificateNumber))
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.RecordId).ToList());
            var ambiguous = grouped.Where(kv => kv.Value.Count > 1).ToList();
            Console.WriteLine($"  distinct certificate numbers: {grouped.Count}");
            if (ambiguous.Count > 0)
                Console.WriteLine($"  numbers with >1 file        : {ambiguous.Count}  (lowest record_id wins; alternates reported)");

            // ---------------------------------------------------------------- certificates
            var certs = await db.Certificates.ToListAsync();
            Console.WriteLine($"Certificates in the database : {certs.Count}");

            var byNumber = new Dictionary<string, Certificate>();
            var byNormalized = new Dictionary<string, Certificate>();
            foreach (var c in certs)
            {
                var k = Key(c.Number);
                if (k.Length > 0) byNumber.TryAdd(k, c);
                // CR-187: fall back to the persisted normalized column. Never add another
                // normalizer — SearchNormalizer is the single source.
                var n = SearchNormalizer.Normalize(c.Number);
                if (!string.IsNullOrEmpty(n)) byNormalized.TryAdd(n, c);
            }

            // ---------------------------------------------------------------- work
            var undo = new List<UndoRow>();
            var unmatched = new List<ReviewRow>();
            var problems = new List<ReviewRow>();
            int linked = 0, wouldLink = 0, alreadyLinked = 0, checksumFailed = 0,
                missingFile = 0, notFound = 0, thumbFailed = 0, processed = 0;

            var work = grouped.OrderBy(kv => kv.Key).ToList();
            if (opts.Limit > 0) work = work.Take(opts.Limit).ToList();
            Console.WriteLine($"Certificate numbers to process: {work.Count}");
            Console.WriteLine();

            foreach (var (key, candidates) in work)
            {
                processed++;
                var row = candidates[0];

                foreach (var alt in candidates.Skip(1))
                    problems.Add(new ReviewRow(alt.CertificateNumber, alt.RecordId, alt.LocalPath,
                        "alternate file for a certificate number already attached from another record"));

                // --- match to a certificate ---
                if (!byNumber.TryGetValue(key, out var cert))
                {
                    var norm = SearchNormalizer.Normalize(row.CertificateNumber);
                    if (string.IsNullOrEmpty(norm) || !byNormalized.TryGetValue(norm, out cert))
                    {
                        notFound++;
                        unmatched.Add(new ReviewRow(row.CertificateNumber, row.RecordId, row.LocalPath,
                            "no certificate with this number in the target database"));
                        continue;
                    }
                }

                // --- already linked? ---
                if (!string.IsNullOrWhiteSpace(cert.CertificateFile) && !opts.Force)
                {
                    alreadyLinked++;
                    continue;
                }

                // --- source file present and intact? ---
                var source = Path.GetFullPath(Path.Combine(manifestDir, row.LocalPath!));
                if (!File.Exists(source))
                {
                    missingFile++;
                    problems.Add(new ReviewRow(row.CertificateNumber, row.RecordId, row.LocalPath,
                        "file listed in the manifest is not on disk"));
                    continue;
                }

                // Checksum BEFORE any write. A truncated or swapped file must never reach the DB —
                // a wrong PDF attached to a certificate is worse than no PDF, because nothing
                // downstream will ever flag it.
                var actual = await Sha256Async(source);
                if (!string.Equals(actual, row.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    checksumFailed++;
                    problems.Add(new ReviewRow(row.CertificateNumber, row.RecordId, row.LocalPath,
                        $"sha256 mismatch — manifest {Short(row.Sha256)}, file {Short(actual)}"));
                    continue;
                }

                if (opts.DryRun)
                {
                    wouldLink++;
                    if (opts.Verbose)
                        Console.WriteLine($"  would link  {cert.Number,-12} <- {Path.GetFileName(source)}");
                    continue;
                }

                // --- copy into the uploads store, naming exactly as the importer does ---
                Directory.CreateDirectory(paths.CertificatesDir);
                var fileName = $"{cert.Number}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                var dest = Path.Combine(paths.CertificatesDir, fileName);
                if (File.Exists(dest))
                {
                    // Same guard as FileUploadService: a second file for the same certificate
                    // inside the same second must not overwrite the first.
                    fileName = $"{cert.Number}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[0..8]}.pdf";
                    dest = Path.Combine(paths.CertificatesDir, fileName);
                }

                var oldFile = cert.CertificateFile;
                var oldImage = cert.CertificateImagePath;

                try
                {
                    File.Copy(source, dest, overwrite: false);
                }
                catch (Exception ex)
                {
                    problems.Add(new ReviewRow(row.CertificateNumber, row.RecordId, row.LocalPath,
                        $"copy failed: {ex.Message}"));
                    continue;
                }

                cert.CertificateFile = Path.Combine("uploads", "certificates", fileName).Replace("\\", "/");

                // Thumbnail. A failure here is NOT fatal — the PDF is still attached and correct —
                // but it is counted and reported, because the visible symptom (blank preview) looks
                // identical to the by-design pre-save blank and would otherwise be misfiled as a bug.
                string? thumb = null;
                try
                {
                    thumb = await uploads.GeneratePdfThumbnailAsync(dest, cert.Number);
                    if (!string.IsNullOrEmpty(thumb)) cert.CertificateImagePath = thumb;
                    else thumbFailed++;
                }
                catch (Exception ex)
                {
                    thumbFailed++;
                    log.LogWarning(ex, "Thumbnail generation failed for {Number}", cert.Number);
                }

                undo.Add(new UndoRow(cert.Id, cert.Number, oldFile, oldImage,
                    cert.CertificateFile, cert.CertificateImagePath, dest));
                linked++;

                // Bounded transactions: a 5,000-row single SaveChanges would hold locks for the
                // whole run and lose everything on one failure.
                if (linked % 100 == 0)
                {
                    await db.SaveChangesAsync();
                    WriteUndo(outDir, stamp, undo);   // flush as we go, so a crash still leaves a rollback
                    Console.WriteLine($"  [{processed}/{work.Count}] linked {linked}, thumbnails failed {thumbFailed}");
                }
            }

            if (!opts.DryRun)
            {
                await db.SaveChangesAsync();
                WriteUndo(outDir, stamp, undo);
            }

            WriteReview(Path.Combine(outDir, $"unmatched-{stamp}.csv"), unmatched);
            WriteReview(Path.Combine(outDir, $"review-{stamp}.csv"), problems);

            // ---------------------------------------------------------------- summary
            var elapsed = DateTime.UtcNow - started;
            Console.WriteLine();
            Console.WriteLine(new string('=', 78));
            Console.WriteLine("SUMMARY");
            Console.WriteLine(new string('=', 78));
            Console.WriteLine($"Certificate numbers processed : {processed}");
            if (opts.DryRun)
                Console.WriteLine($"  would link                  : {wouldLink}");
            else
                Console.WriteLine($"  linked                      : {linked}");
            Console.WriteLine($"  already had a file (skipped)  : {alreadyLinked}");
            Console.WriteLine($"  no matching certificate       : {notFound}");
            Console.WriteLine($"  manifest file missing on disk : {missingFile}");
            Console.WriteLine($"  sha256 mismatch (not linked)  : {checksumFailed}");
            if (!opts.DryRun)
                Console.WriteLine($"  linked but thumbnail failed   : {thumbFailed}");
            Console.WriteLine($"Elapsed                       : {elapsed.TotalMinutes:F1} min");
            Console.WriteLine();
            if (!opts.DryRun && undo.Count > 0)
            {
                Console.WriteLine($"Undo CSV : {Path.Combine(outDir, $"undo-{stamp}.csv")}");
                Console.WriteLine("  Rollback = restore those two columns and delete copied_to files.");
            }
            if (unmatched.Count > 0) Console.WriteLine($"Unmatched: {Path.Combine(outDir, $"unmatched-{stamp}.csv")}  ({unmatched.Count} rows)");
            if (problems.Count > 0) Console.WriteLine($"Review   : {Path.Combine(outDir, $"review-{stamp}.csv")}  ({problems.Count} rows)");

            // Non-zero only for things that need a human. Unmatched numbers are expected (the pull
            // is a superset of the load set), so they do not fail the run.
            return (checksumFailed + missingFile) > 0 ? 1 : 0;
        }

        // ------------------------------------------------------------------ helpers

        private const string Usage = """
            Usage:
              dotnet run -- link-pdfs --manifest <path to manifest.csv> [options]

            Options:
              --manifest <path>   manifest.csv from the portal pull (required)
              --dry-run           report what would be linked; no files copied, no DB writes
              --force             re-link certificates that already have a file
              --status <name>     only this certificate status (repeatable, exact match)
              --limit <n>         only the first n certificate numbers (testing)
              --out <dir>         where to write undo/review CSVs (default: <manifest dir>\\_link-pdfs)
              --verbose           list every certificate in dry-run
            """;

        private static string Key(string? number) => (number ?? "").Trim().ToUpperInvariant();

        private static string Short(string? hash) =>
            string.IsNullOrEmpty(hash) ? "(none)" : hash[..Math.Min(12, hash.Length)];

        private static async Task<string> Sha256Async(string path)
        {
            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                                bufferSize: 1 << 20, useAsync: true);
            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(fs);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static List<ManifestRow> ReadManifest(string path)
        {
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                PrepareHeaderForMatch = a => a.Header.Trim().ToLowerInvariant(),
            };
            using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, cfg);
            return csv.GetRecords<ManifestRow>().ToList();
        }

        private static void WriteUndo(string dir, string stamp, List<UndoRow> undo)
        {
            if (undo.Count == 0) return;
            using var w = new StreamWriter(Path.Combine(dir, $"undo-{stamp}.csv"), false);
            using var csv = new CsvWriter(w, CultureInfo.InvariantCulture);
            csv.WriteRecords(undo);
        }

        private static void WriteReview(string path, List<ReviewRow> rows)
        {
            if (rows.Count == 0) return;
            using var w = new StreamWriter(path, false);
            using var csv = new CsvWriter(w, CultureInfo.InvariantCulture);
            csv.WriteRecords(rows);
        }

        // ------------------------------------------------------------------ types

        private sealed class ManifestRow
        {
            [Name("certificate_number")] public string? CertificateNumber { get; set; }
            [Name("local_path")] public string? LocalPath { get; set; }
            [Name("sha256")] public string? Sha256 { get; set; }
            [Name("cert_status")] public string? CertStatus { get; set; }
            [Name("record_id")] public int RecordId { get; set; }
        }

        private sealed record UndoRow(
            int CertificateId, string Number,
            string? PreviousCertificateFile, string? PreviousImagePath,
            string? NewCertificateFile, string? NewImagePath,
            string CopiedTo);

        private sealed record ReviewRow(string? CertificateNumber, int RecordId, string? LocalPath, string Reason);

        private sealed class Options
        {
            public string ManifestPath { get; private set; } = "";
            public string? OutDir { get; private set; }
            public bool DryRun { get; private set; }
            public bool Force { get; private set; }
            public bool Verbose { get; private set; }
            public int Limit { get; private set; }
            public HashSet<string> Statuses { get; } = new(StringComparer.Ordinal);
            public string? Error { get; private set; }

            public static Options Parse(string[] args)
            {
                var o = new Options();
                for (var i = 1; i < args.Length; i++)
                {
                    switch (args[i].ToLowerInvariant())
                    {
                        case "--manifest" when i + 1 < args.Length: o.ManifestPath = args[++i]; break;
                        case "--out" when i + 1 < args.Length: o.OutDir = args[++i]; break;
                        case "--status" when i + 1 < args.Length: o.Statuses.Add(args[++i]); break;
                        case "--limit" when i + 1 < args.Length:
                            if (int.TryParse(args[++i], out var n)) o.Limit = n;
                            else o.Error = "--limit needs a number";
                            break;
                        case "--dry-run": o.DryRun = true; break;
                        case "--force": o.Force = true; break;
                        case "--verbose": o.Verbose = true; break;
                        default: o.Error = $"unrecognised argument: {args[i]}"; break;
                    }
                    if (o.Error != null) return o;
                }

                if (string.IsNullOrWhiteSpace(o.ManifestPath)) o.Error = "--manifest is required";
                else if (!File.Exists(o.ManifestPath)) o.Error = $"manifest not found: {o.ManifestPath}";
                return o;
            }
        }
    }
}
