/**
 * Shared CSV parsing for every client-side importer (DEF-419).
 *
 * Why this file exists: each importer used to do `contents.split('\n')` and then hand each
 * line to its own quote-aware `parseCSVLine()`. The per-line parser was fine; the file-level
 * split was not. A CSV field may contain a hard line break as long as it is quoted
 * (RFC 4180 2.6) — exactly what Excel writes when a cell holds an Alt+Enter break.
 * Splitting on every '\n' tore such a record in half:
 *
 *   ASIN,B0D93WK4JX,"Satisfry Panoramic 5L Air Fryer Cream
 *   (Same as 27420-56 except in Cream color)",,Amazon,
 *
 * became a 3-field row that imported with a truncated name and NO partner (the Partner column
 * now sits past the end of the row), plus an orphan tail that the `fields.length < 2` guard
 * dropped without a word. Both halves failed silently and the import still reported success.
 *
 * `splitRecords()` only ends a record on a newline that is OUTSIDE quotes, so the record stays
 * whole and the columns stay aligned. Loaded globally from _Layout.cshtml.
 */
(function (global) {
    'use strict';

    /**
     * Split raw CSV text into records, honouring quoted fields that span lines.
     * Handles LF, CRLF and CR line endings, and the "" escape for a literal quote.
     * Quote characters are preserved in the returned records so parseLine() can interpret them.
     * Blank records are dropped, matching the `.filter(line => line.trim())` the callers used.
     *
     * @param {string} text raw file contents
     * @returns {string[]} one entry per logical CSV record
     */
    function splitRecords(text) {
        const records = [];
        let current = '';
        let inQuotes = false;

        for (let i = 0; i < text.length; i++) {
            const char = text[i];

            if (char === '"') {
                // "" inside a quoted field is an escaped quote, not the end of the field.
                // Consuming both here keeps inQuotes correct; parseLine() unescapes it.
                if (inQuotes && text[i + 1] === '"') {
                    current += '""';
                    i++;
                    continue;
                }
                inQuotes = !inQuotes;
                current += char;
            } else if ((char === '\n' || char === '\r') && !inQuotes) {
                if (char === '\r' && text[i + 1] === '\n') i++; // CRLF counts once
                records.push(current);
                current = '';
            } else {
                current += char;
            }
        }

        if (current.length > 0) records.push(current);
        return records.filter(function (r) { return r.trim().length > 0; });
    }

    /**
     * Split ONE record into fields. Surrounding quotes are stripped, "" becomes a literal
     * quote, and each field is trimmed — the behaviour every importer's local parseCSVLine()
     * already had, minus the lost "" escape.
     *
     * @param {string} line a single record from splitRecords()
     * @returns {string[]} field values
     */
    function parseLine(line) {
        const fields = [];
        let current = '';
        let inQuotes = false;

        for (let i = 0; i < line.length; i++) {
            const char = line[i];

            if (char === '"') {
                if (inQuotes && line[i + 1] === '"') {
                    current += '"'; // escaped quote -> literal
                    i++;
                } else {
                    inQuotes = !inQuotes;
                }
            } else if (char === ',' && !inQuotes) {
                fields.push(current.trim());
                current = '';
            } else {
                current += char;
            }
        }

        fields.push(current.trim());
        return fields;
    }

    /**
     * Escape ONE value for writing into a CSV file (DEF-420).
     *
     * The inverse of parseLine(), and the half that was missing: several exporters wrapped
     * values in quotes without doubling the quotes inside them, so a value containing a "
     * produced an unbalanced record. Re-importing that file collapsed everything after the
     * quote into one field and dropped the columns that followed - the same silent Partner
     * loss as DEF-419, arriving backwards through export -> edit -> re-import.
     *
     * @param {*} value the raw cell value
     * @param {boolean} [forceQuote] always wrap in quotes, even when not strictly required.
     *        Callers that already quote every field pass true so the file they emit keeps its
     *        exact shape - notably identifier columns, where dropping the quotes would let
     *        Excel turn a UPC like 012345678905 into a number and eat the leading zero.
     * @returns {string} the field, ready to join with commas
     */
    function escapeField(value, forceQuote) {
        const str = value === null || value === undefined ? '' : String(value);
        const escaped = str.replace(/"/g, '""');
        // A value holding a comma, a quote, or a line break MUST be quoted (RFC 4180).
        if (forceQuote || /[",\r\n]/.test(str)) {
            return '"' + escaped + '"';
        }
        return escaped;
    }

    global.GccCsv = { splitRecords: splitRecords, parseLine: parseLine, escapeField: escapeField };

    // Also expose to Node so Scripts/ci/check-csv-import.js can regression-test this exact file
    // rather than a copy of it that can drift.
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = global.GccCsv;
    }
})(typeof window !== 'undefined' ? window : globalThis);
