/**
 * DEF-419 regression check — CSV import must not lose rows or shift columns.
 *
 * Run:  node Scripts/ci/check-csv-import.js
 *
 * Loads the REAL wwwroot/js/csv-parse.js (not a copy) so this check and the shipped parser
 * can never drift apart. The headline fixture is the client file that exposed the defect:
 * an ASIN sheet where one Product Name cell held an Alt+Enter line break, which made that
 * row import with no Amazon partner tag while a second row vanished entirely — silently,
 * with the import still reporting success.
 */
'use strict';

const path = require('path');
const GccCsv = require(path.join(__dirname, '..', '..', 'wwwroot', 'js', 'csv-parse.js'));

const CR = String.fromCharCode(13);
let failures = 0;

function check(name, actual, expected) {
    const a = JSON.stringify(actual);
    const e = JSON.stringify(expected);
    if (a === e) {
        console.log('  PASS  ' + name);
    } else {
        failures++;
        console.log('  FAIL  ' + name);
        console.log('        expected: ' + e);
        console.log('        actual:   ' + a);
    }
}

/** Mirrors the importer: records -> header map -> the Partner value for each row. */
function importRows(csv) {
    const records = GccCsv.splitRecords(csv);
    const headers = GccCsv.parseLine(records[0]).map(h => h.trim().toLowerCase());
    // DEF-342: the shipped template heading reads "Partner (separate by comma)".
    const partnerHeader = headers.find(h => h === 'partner' || h.startsWith('partner ')) || 'partner';
    return records.slice(1).map(r => {
        const fields = GccCsv.parseLine(r);
        const row = {};
        // DEF-423: mirrors product-id-manager.js, which strips the Excel text-format wrapper
        // from every column as it reads it. Keep these two in step - the whole value of this
        // harness is that it models the real importer rather than an idealised one.
        headers.forEach((h, i) => {
            row[h] = fields[i] !== undefined ? GccCsv.stripExcelText(fields[i]) : '';
        });
        return {
            value: row['product id'] || '',
            name: row['product name'] || '',
            partner: row[partnerHeader] || '',
            columns: fields.length
        };
    });
}

console.log('DEF-419 / DEF-420 CSV import + export regression check');
console.log('');

// ---------------------------------------------------------------------------
// 1. The reported defect: an Alt+Enter line break inside a quoted Product Name.
// ---------------------------------------------------------------------------
const danFile =
`Type,Product ID,Product Name,SKU,Partner (separate by comma),Description
ASIN,B0B915YZD5,Satisfry Air 5L Air Fryer,,Amazon,
ASIN,B0D93YP9NM,Satisfry Panoramic 5L Air Fryer Black,,Amazon,
ASIN,B0D93WK4JX,"Satisfry Panoramic 5L Air Fryer Cream
(Same as 27420-56 except in Cream color)",,Amazon,
ASIN,B0B2DPK52Y,Satisfry Air 5L Air Fryer,,Amazon,
`;

const rows = importRows(danFile);
check('multi-line cell: no row is lost', rows.length, 4);
check('multi-line cell: every row keeps its partner',
    rows.map(r => r.partner), ['Amazon', 'Amazon', 'Amazon', 'Amazon']);
check('multi-line cell: the ASINs are the four from the file',
    rows.map(r => r.value), ['B0B915YZD5', 'B0D93YP9NM', 'B0D93WK4JX', 'B0B2DPK52Y']);
check('multi-line cell: the name keeps both of its lines',
    rows[2].name.indexOf('Same as 27420-56') > -1, true);
check('multi-line cell: no column shift',
    rows.map(r => r.columns), [6, 6, 6, 6]);

// ---------------------------------------------------------------------------
// 2. A comma inside a quoted field must not shift the columns after it.
// ---------------------------------------------------------------------------
const commaFile =
`Type,Product ID,Product Name,SKU,Partner,Description
ASIN,B0AAAAAAAA,"Fryer, 5L, Black",SKU-1,Amazon,note
`;
const commaRows = importRows(commaFile);
check('quoted comma: partner stays in its column', commaRows[0].partner, 'Amazon');
check('quoted comma: name keeps its commas', commaRows[0].name, 'Fryer, 5L, Black');

// ---------------------------------------------------------------------------
// 3. Doubled quotes are a literal quote, not a field boundary.
// ---------------------------------------------------------------------------
const quoteFile =
`Type,Product ID,Product Name,SKU,Partner,Description
ASIN,B0BBBBBBBB,"Fryer 5"" deep",SKU-2,Amazon,note
`;
const quoteRows = importRows(quoteFile);
check('escaped quote: value unescaped correctly', quoteRows[0].name, 'Fryer 5" deep');
check('escaped quote: partner stays in its column', quoteRows[0].partner, 'Amazon');

// ---------------------------------------------------------------------------
// 4. Line endings: CRLF (Excel on Windows) and a CR inside a quoted cell.
// ---------------------------------------------------------------------------
const crlfFile =
    'Type,Product ID,Product Name,SKU,Partner,Description' + CR + '\n' +
    'ASIN,B0CCCCCCCC,"Line one' + CR + '\nLine two",SKU-3,Amazon,note' + CR + '\n';
const crlfRows = importRows(crlfFile);
check('CRLF: one record, not two', crlfRows.length, 1);
check('CRLF: partner intact', crlfRows[0].partner, 'Amazon');

// ---------------------------------------------------------------------------
// 5. The Partner heading variants the templates actually ship with.
// ---------------------------------------------------------------------------
const plainHeader =
`Type,Product ID,Product Name,SKU,Partner,Description
ASIN,B0DDDDDDDD,Widget,,Novi,
`;
check('plain "Partner" heading still resolves', importRows(plainHeader)[0].partner, 'Novi');

// ---------------------------------------------------------------------------
// 6. Trailing newline / blank lines must not produce phantom rows.
// ---------------------------------------------------------------------------
const trailing =
`Type,Product ID,Product Name,SKU,Partner,Description
ASIN,B0EEEEEEEE,Widget,,Amazon,


`;
check('blank trailing lines produce no phantom rows', importRows(trailing).length, 1);


// ---------------------------------------------------------------------------
// 7. DEF-420 ROUND TRIP: what we export must re-import to the same values.
//    The Product ID exporter used to interpolate values straight into quotes,
//    so a name containing a double-quote emitted an unbalanced record and the
//    Partner was lost on the way back in - the DEF-419 symptom, backwards.
// ---------------------------------------------------------------------------
const NL = String.fromCharCode(10);
const EXPORT_HEADER = 'Type,Product ID,Product Name,SKU,Partner,Created,Created By,Description';

/** Mirrors product-id-manager.js exportToCSV(): every field quoted, via escapeField. */
function exportRow(pid) {
    return [
        pid.productIdType, pid.productIdValue, pid.productName, pid.productSku,
        pid.partners.join(', '), pid.created, pid.by, pid.description
    ].map(v => GccCsv.escapeField(v, true)).join(',');
}

const nasty = [
    { label: 'double-quote in name',
      pid: { productIdType:'ASIN', productIdValue:'B0D93WK4JX', productName:'Satisfry 5" Panoramic',
             productSku:'', partners:['Amazon'], created:'2026-09-10', by:'dan', description:'' } },
    { label: 'comma in name',
      pid: { productIdType:'ASIN', productIdValue:'B0AAAAAAAA', productName:'Fryer, 5L, Black',
             productSku:'', partners:['Amazon'], created:'2026-09-10', by:'dan', description:'' } },
    { label: 'line break in name',
      pid: { productIdType:'ASIN', productIdValue:'B0BBBBBBBB',
             productName:'Cream' + NL + '(Same as 27420-56)',
             productSku:'', partners:['Amazon'], created:'2026-09-10', by:'dan', description:'' } },
    { label: 'multi-partner + quote + comma',
      pid: { productIdType:'ASIN', productIdValue:'B0CCCCCCCC', productName:'A "B", C',
             productSku:'SKU,1', partners:['Amazon','Novi'], created:'2026-09-10', by:'dan', description:'n/a' } }
];

for (const t of nasty) {
    const csv = EXPORT_HEADER + NL + exportRow(t.pid) + NL;
    const back = importRows(csv);
    check('round trip (' + t.label + '): exactly one row', back.length, 1);
    check('round trip (' + t.label + '): partner survives', back[0].partner, t.pid.partners.join(', '));
    check('round trip (' + t.label + '): name survives', back[0].name, t.pid.productName);
}

// A leading zero on an identifier must not be exported bare (Excel would eat it).
check('identifier column stays quoted', GccCsv.escapeField('012345678905', true), '"012345678905"');
// Without forceQuote a plain value stays bare, so conditional exporters keep their shape.
check('plain value stays unquoted when not forced', GccCsv.escapeField('Amazon'), 'Amazon');

// ---------------------------------------------------------------------------------------
// DEF-423: formula-injection guard, and the strip that makes it safe.
//
// A value starting = + - @ is evaluated as a formula by Excel/Sheets - that is how the
// certification type "+Vantage Vinyl Verified" came to display as #NAME? (DEF-306). It is
// now exported in Excel text format. The pairing is the point: the guard is only safe
// because stripExcelText() undoes it on re-import, so these assert the ROUND TRIP, not the
// encoding. Asserting only the encoding would let someone "fix" one half and ship a
// silent corruption in the other.
// ---------------------------------------------------------------------------------------
const formulaCases = [
    '+Vantage Vinyl Verified',   // the real DEF-306 string, still live on 1 cert type / 78 products
    '=cmd|calc',                 // the injection shape the guard exists to neutralise
    '-1234',
    '@handle',
    '=a"b,c',                    // guard + embedded quote + embedded comma together
];
for (const v of formulaCases) {
    const encoded = GccCsv.escapeField(v, true);
    check('formula guard quotes "' + v + '"', encoded[0], '"');
    check('formula guard round-trips "' + v + '"', GccCsv.stripExcelText(GccCsv.parseLine(encoded)[0]), v);
}

// The guard must not disturb ordinary values.
check('plain value untouched by guard', GccCsv.escapeField('Vantage Vinyl Verified'), 'Vantage Vinyl Verified');
check('quote-only value still plain-quoted', GccCsv.escapeField('Satisfry 5" Panoramic', true), '"Satisfry 5"" Panoramic"');

// stripExcelText must leave a normal value alone, including one that merely CONTAINS '='.
check('strip leaves normal value', GccCsv.stripExcelText('Acme = Best'), 'Acme = Best');
check('strip leaves empty', GccCsv.stripExcelText(''), '');
// Bare-equals residue is opt-in only: safe for identifiers, refused by default so a company
// name legitimately beginning "=" is not silently truncated.
check('bare equals kept by default', GccCsv.stripExcelText('=NotAWrapper'), '=NotAWrapper');
check('bare equals dropped when asked', GccCsv.stripExcelText('=012345', true), '012345');

// Full export -> re-import through the certificate importer shape, with a formula value in
// the name column: the row must survive intact and keep its partner.
{
    const csv = EXPORT_HEADER + NL + exportRow({
        productIdType: 'ASIN', productIdValue: 'B0DDDDDDDD', productName: '+Vantage Vinyl Verified',
        productSku: 'SKU-1', partners: ['Amazon'], created: '2026-09-11', by: 'dan', description: ''
    }) + NL;
    const back = importRows(csv);
    check('formula value: exactly one row', back.length, 1);
    check('formula value: partner survives', back[0].partner, 'Amazon');
    check('formula value: name survives', back[0].name, '+Vantage Vinyl Verified');
}

console.log('');
if (failures > 0) {
    console.log('DEF-419/420/423 check FAILED (' + failures + ' assertion(s))');
    process.exit(1);
}
console.log('DEF-419/420/423 check PASSED (all assertions)');
