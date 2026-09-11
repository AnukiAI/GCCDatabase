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
        headers.forEach((h, i) => { row[h] = fields[i] !== undefined ? fields[i] : ''; });
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

console.log('');
if (failures > 0) {
    console.log('DEF-419/420 check FAILED (' + failures + ' assertion(s))');
    process.exit(1);
}
console.log('DEF-419/420 check PASSED (all assertions)');
