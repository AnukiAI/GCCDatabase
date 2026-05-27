// Minimal MD→HTML converter for the dashboard matrix file.
// Handles: H1/H2/H3, paragraphs, ul lists, tables (pipe syntax), bold,
// code spans, blockquote-style ✓/✗ glyphs. No external deps.

const fs = require('fs');
const path = require('path');

const IN = process.argv[2];
if (!IN) { console.error('usage: node render-matrix-html.js <input.md>'); process.exit(1); }
const md = fs.readFileSync(IN, 'utf8');
const OUT = IN.replace(/\.md$/i, '.html');

function escapeHtml(s) {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}

function inline(s) {
  // escape first, then re-introduce inline markdown
  s = escapeHtml(s);
  // code spans `…`
  s = s.replace(/`([^`]+)`/g, '<code>$1</code>');
  // bold **…**
  s = s.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
  // italics *…* (single, after bold)
  s = s.replace(/(^|[^*])\*([^*]+)\*/g, '$1<em>$2</em>');
  // links [text](url) — defensive only
  s = s.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>');
  return s;
}

const lines = md.split(/\r?\n/);
const out = [];
let i = 0;
function flush() {} // placeholder if we ever batch

while (i < lines.length) {
  const line = lines[i];

  // Tables — header row | --- row | data rows
  if (/^\s*\|.*\|\s*$/.test(line) && /^\s*\|[-:\s|]+\|\s*$/.test(lines[i+1] || '')) {
    const head = line.trim().slice(1, -1).split('|').map(c => c.trim());
    i += 2; // skip header + separator
    const body = [];
    while (i < lines.length && /^\s*\|.*\|\s*$/.test(lines[i])) {
      body.push(lines[i].trim().slice(1, -1).split('|').map(c => c.trim()));
      i++;
    }
    out.push('<table>');
    out.push('  <thead><tr>' + head.map(h => `<th>${inline(h)}</th>`).join('') + '</tr></thead>');
    out.push('  <tbody>');
    for (const row of body) {
      // colorize result glyphs
      const cells = row.map(c => {
        let cls = '';
        if (/^✅$/.test(c)) cls = ' class="ok"';
        else if (/^❌/.test(c)) cls = ' class="fail"';
        else if (c === '—') cls = ' class="na"';
        else if (c === '✓') cls = ' class="ok"';
        return `<td${cls}>${inline(c)}</td>`;
      });
      out.push('    <tr>' + cells.join('') + '</tr>');
    }
    out.push('  </tbody>');
    out.push('</table>');
    continue;
  }

  // Headings
  let m;
  if ((m = /^### (.+)$/.exec(line))) { out.push(`<h3>${inline(m[1])}</h3>`); i++; continue; }
  if ((m = /^## (.+)$/.exec(line))) { out.push(`<h2>${inline(m[1])}</h2>`); i++; continue; }
  if ((m = /^# (.+)$/.exec(line))) { out.push(`<h1>${inline(m[1])}</h1>`); i++; continue; }

  // Bulleted lists
  if (/^\s*[-*] /.test(line)) {
    out.push('<ul>');
    while (i < lines.length && /^\s*[-*] /.test(lines[i])) {
      const item = lines[i].replace(/^\s*[-*] /, '');
      out.push(`  <li>${inline(item)}</li>`);
      i++;
    }
    out.push('</ul>');
    continue;
  }

  // Blank line
  if (/^\s*$/.test(line)) { i++; continue; }

  // Numbered lists (simple)
  if (/^\s*\d+\.\s/.test(line)) {
    out.push('<ol>');
    while (i < lines.length && /^\s*\d+\.\s/.test(lines[i])) {
      const item = lines[i].replace(/^\s*\d+\.\s/, '');
      out.push(`  <li>${inline(item)}</li>`);
      i++;
    }
    out.push('</ol>');
    continue;
  }

  // Paragraph
  out.push(`<p>${inline(line)}</p>`);
  i++;
}

const html = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Dashboard Data-Accuracy Matrix</title>
<style>
  body { font-family: -apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif; max-width: 1600px; margin: 24px auto; padding: 0 24px; color: #222; line-height: 1.5; }
  h1 { color: #1a3a5c; border-bottom: 3px solid #0099d6; padding-bottom: 8px; }
  h2 { color: #1a3a5c; border-bottom: 1px solid #ccc; padding-bottom: 4px; margin-top: 32px; }
  h3 { color: #444; margin-top: 24px; }
  table { border-collapse: collapse; width: 100%; margin: 12px 0 24px 0; font-size: 13px; }
  th { background: #f0f4f8; text-align: left; padding: 8px 10px; border-bottom: 2px solid #b0c0d0; white-space: nowrap; font-weight: 700; color: #1a3a5c; }
  td { padding: 6px 10px; border-bottom: 1px solid #e8eef2; vertical-align: top; }
  tbody tr:hover { background: #f8fafd; }
  td.ok { color: #1c7a3c; font-weight: 700; text-align: center; }
  td.fail { color: #b00020; font-weight: 700; text-align: center; }
  td.na { color: #888; text-align: center; }
  code { background: #f4f6f8; padding: 1px 6px; border-radius: 3px; font-size: 0.92em; color: #333; }
  ul, ol { margin: 8px 0 16px 0; padding-left: 28px; }
  li { margin: 4px 0; }
  p { margin: 8px 0; }
  strong { color: #1a3a5c; }
  /* Wide per-cert matrix needs horizontal scroll without page-wide overflow */
  table { display: block; overflow-x: auto; }
  thead, tbody { display: table; width: 100%; }
  /* Subtle row banding for the per-cert matrix */
  tbody tr:nth-child(even) { background: #fafbfc; }
  /* Status column highlight */
  td:nth-child(2) { font-size: 12px; color: #555; }
</style>
</head>
<body>
${out.join('\n')}
<hr>
<p style="color:#888;font-size:12px;">Generated from <code>${path.basename(IN)}</code> at ${new Date().toLocaleString()}</p>
</body>
</html>`;

fs.writeFileSync(OUT, html);
console.log(`Wrote ${OUT}`);
