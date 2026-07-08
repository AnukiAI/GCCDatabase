// Write-path harness — helpers for CRUD round-trip specs (Phase 3 of the regression strategy).
//
// SAFETY MODEL (non-negotiable — this suite WRITES to the dev DB):
//   - Operates ONLY on self-created fixtures marked ZZ_SmokeFixture_* (feedback_prelaunch_test_env
//     + the _views/ state-matrix ZZ_SmokeFixture_* convention). Never touches real business rows.
//   - Reuses EXISTING company/product/type combos (DB-verified) so CR-186 find-or-create RESOLVES
//     rather than creating new product rows — and asserts a product-count invariant to prove it.
//   - Guaranteed teardown in finally (app Delete on the happy path + SQL backstop by marker).
//
// Reuses _security/_lib/security-harness for login/context. Adds: sqlcmd access (same parsing as
// _views/cert-state-matrix.js) + form scraping + antiforgery-aware form POST.

const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const H = require('../../_security/_lib/security-harness');

const FIXTURE_MARKER = 'ZZ_SmokeFixture_CRUD';

// --- sqlcmd (dev DB) — connection from env (CI) or appsettings.Development.json (local) ---
// CI sets SMOKE_DB_CONN (or ConnectionStrings__DefaultConnection) to point the write-path suite at
// the CI SQL Server, matching the app's own connection. Locally it falls back to the dev config so
// nothing changes for the normal workflow.
function connString() {
  const env = process.env.SMOKE_DB_CONN || process.env.ConnectionStrings__DefaultConnection;
  if (env) return env;
  // __dirname = Scripts/smoke-prod/_crud/_lib -> up 4 to the app root.
  const cfgPath = path.resolve(__dirname, '..', '..', '..', '..', 'appsettings.Development.json');
  return JSON.parse(fs.readFileSync(cfgPath, 'utf8')).ConnectionStrings.DefaultConnection;
}

function sqlcmdArgs() {
  const conn = connString();
  const kv = {};
  for (const part of conn.split(';')) {
    const i = part.indexOf('=');
    if (i > 0) kv[part.slice(0, i).trim().toLowerCase()] = part.slice(i + 1).trim();
  }
  return ['-S', kv['server'], '-d', kv['database'], '-U', kv['user id'], '-P', kv['password'], '-C', '-b'];
}

// CR-187: Certificates has persisted computed Normalized* columns + filtered indexes, so any
// statement touching it needs QUOTED_IDENTIFIER ON (same requirement the state-matrix seed noted).
const SET_OPTS = 'SET QUOTED_IDENTIFIER ON; SET NOCOUNT ON;';

// Run a query, return array of rows (each row = array of trimmed column strings). Pipe-separated.
function sqlQuery(sql) {
  const out = execFileSync('sqlcmd',
    [...sqlcmdArgs(), '-h', '-1', '-W', '-s', '|', '-Q', `${SET_OPTS} ${sql}`],
    { encoding: 'utf8' });
  return out.split(/\r?\n/)
    .map((l) => l.trim())
    .filter((l) => l && !/^\(\d+ rows? affected\)$/.test(l))
    .map((l) => l.split('|').map((c) => c.trim()));
}

function sqlScalar(sql) {
  const rows = sqlQuery(sql);
  return rows.length ? rows[0][0] : null;
}

function sqlExec(sql) {
  execFileSync('sqlcmd', [...sqlcmdArgs(), '-Q', `${SET_OPTS} ${sql}`], { stdio: 'pipe' });
}

// --- form helpers ---
// Scrape a rendered form into {action, method, fields:{name->value}} — includes hidden inputs,
// the antiforgery token, and __Invariant, so a re-POST mimics a real browser submit.
async function scrapeForm(page, formSelector = 'form') {
  return page.evaluate((sel) => {
    const form = document.querySelector(sel);
    if (!form) return null;
    const fields = {};
    form.querySelectorAll('[name]').forEach((el) => {
      const name = el.getAttribute('name');
      if (el.type === 'file' || el.type === 'submit' || el.type === 'button') return;
      if (el.type === 'checkbox' || el.type === 'radio') {
        if (el.checked) fields[name] = el.value;
        return;
      }
      // last-wins is fine for our single-value fields
      fields[name] = el.value ?? '';
    });
    return { action: form.getAttribute('action') || '', method: (form.getAttribute('method') || 'post').toUpperCase(), fields };
  }, formSelector);
}

// POST a field dict as multipart/form-data through the context (carries antiforgery cookie).
// maxRedirects:0 so we can read the Location header (the new/updated cert number).
async function postForm(ctx, url, fields) {
  const multipart = {};
  for (const [k, v] of Object.entries(fields)) multipart[k] = v == null ? '' : String(v);
  return ctx.request.post(H.BASE + url, { multipart, maxRedirects: 0, failOnStatusCode: false });
}

// The admin JSON APIs (tags/companies/products/users) validate antiforgery via the
// `RequestVerificationToken` HEADER sourced from the hidden input. Load any authed page that
// renders the form, read the token; the antiforgery cookie rides in the shared context jar.
async function antiforgeryToken(page, url) {
  if (url) await page.goto(H.BASE + url, { waitUntil: 'domcontentloaded' });
  return page.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => '');
}

// JSON request through the context (Content-Type application/json + antiforgery header).
// Returns { status, json } — json is null if the body isn't JSON.
async function jsonReq(ctx, method, url, body, token) {
  const resp = await ctx.request.fetch(H.BASE + url, {
    method,
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' },
    data: body === undefined ? undefined : JSON.stringify(body),
    failOnStatusCode: false,
    maxRedirects: 0,
  });
  let json = null;
  try { json = await resp.json(); } catch { /* non-JSON */ }
  return { status: resp.status(), json };
}

module.exports = {
  ...H, FIXTURE_MARKER, sqlQuery, sqlScalar, sqlExec, scrapeForm, postForm, antiforgeryToken, jsonReq,
};
