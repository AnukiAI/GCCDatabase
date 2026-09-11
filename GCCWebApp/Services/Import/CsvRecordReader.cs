using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace GCCWebApp.Services.Import
{
    /// <summary>
    /// DEF-419: one place that knows how to turn an uploaded CSV into records.
    ///
    /// Every hand-rolled importer in this codebase made the same mistake: a quote-aware
    /// field splitter fed by a <c>ReadLineAsync()</c> loop that was not. A CSV field may
    /// contain a hard line break so long as it is quoted (RFC 4180 section 2.6) — exactly
    /// what Excel writes for an Alt+Enter break — so such a record was torn in two. The
    /// first fragment imported with every column after the break shifted or blank; the
    /// second was either rejected as "insufficient fields" or dropped in silence.
    ///
    /// <see cref="CsvParser"/> handles this correctly. <c>parser.Record</c> is the direct
    /// replacement for the old <c>ParseCsvLine(line)</c> — same positional string array,
    /// same per-field trimming.
    /// </summary>
    public static class CsvRecordReader
    {
        /// <summary>
        /// A parser over <paramref name="reader"/> that yields whole records.
        /// Lenient by design: the importers here report their own per-row errors and must
        /// not have an entire file rejected because one row is ragged or one field is odd.
        /// </summary>
        public static CsvParser Create(TextReader reader)
        {
            return new CsvParser(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                DetectDelimiter = false,
                BadDataFound = null,        // never abort the whole file on one odd field
                IgnoreBlankLines = true
            });
        }
    }
}
