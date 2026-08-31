// Read an ASP.NET Core appsettings file from Node.
//
// These files are JSONC, not JSON. .NET's configuration provider accepts `//` and `/* */`
// comments, and this project uses them heavily — several config values carry multi-paragraph
// explanations of outages they exist to prevent, which is exactly where that documentation
// belongs. `JSON.parse` rejects all of it:
//
//   SyntaxError: Expected property name or '}' in JSON at position 888
//
// That is not a hypothetical. `appsettings.Development.json` gained a DEF-286 comment block on
// 2026-08-13, and from that moment every suite that read the file through `JSON.parse` failed
// before it ran a single assertion — the whole CRUD gate reported 0 PASS / 5 FAIL, which reads
// like five broken write paths rather than one unreadable config file. Found 2026-08-25.
//
// THE STRIPPER MUST BE STRING-AWARE. A naive `replace(/\/\/.*$/gm, '')` destroys the very file it
// is meant to read: `"Url": "http://localhost:5101"` contains `//` inside a string literal, and
// blanking from there leaves `"Url": "http:` — an unterminated string. Same for any URL in the
// config. So the scanner tracks whether it is inside a string, and honours backslash escapes.
'use strict';
const fs = require('fs');

// Strip line comments and block comments from JSONC, leaving string literals untouched.
function stripJsonComments(text) {
  let out = '';
  let inString = false, inLine = false, inBlock = false, escaped = false;

  for (let i = 0; i < text.length; i++) {
    const c = text[i], next = text[i + 1];

    if (inLine) {                       // ends at the newline, which we keep so line numbers hold
      if (c === '\n') { inLine = false; out += c; }
      continue;
    }
    if (inBlock) {
      if (c === '*' && next === '/') { inBlock = false; i++; }
      else if (c === '\n') out += c;    // preserve line count for error messages
      continue;
    }
    if (inString) {
      out += c;
      if (escaped) escaped = false;
      else if (c === '\\') escaped = true;
      else if (c === '"') inString = false;
      continue;
    }
    // not in a string or comment
    if (c === '"') { inString = true; out += c; continue; }
    if (c === '/' && next === '/') { inLine = true; i++; continue; }
    if (c === '/' && next === '*') { inBlock = true; i++; continue; }
    out += c;
  }
  return out;
}

/** Read and parse an appsettings-style JSONC file. Throws with the path named on failure. */
function readJsonc(filePath) {
  const raw = fs.readFileSync(filePath, 'utf8');
  try {
    return JSON.parse(stripJsonComments(raw));
  } catch (e) {
    throw new Error(`${filePath} is not parseable even after stripping comments: ${e.message}`);
  }
}

module.exports = { readJsonc, stripJsonComments };
