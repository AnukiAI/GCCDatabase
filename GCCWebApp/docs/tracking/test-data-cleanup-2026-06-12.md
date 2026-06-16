# Test-data cleanup — gccwebapp.com (2026-06-12)

Low priority — this is the **pre-launch test environment**, so these are harmless. Noting for tidiness so they don't get mistaken for real records later.

Created while diagnosing the PDF-upload issues (DEF-261 / DEF-262) on gccwebapp.com.

| Cert # | What it is | Status |
|--------|------------|--------|
| **26-0176** | Test cert created during the Create-flow repro; had a placeholder test PDF | ✅ **Deleted 2026-06-12** (via app, confirmed 404) |
| **26-0177** | Test cert created during the Create-flow repro; **empty** (native submit bypassed the required-PDF check) | ✅ **Deleted 2026-06-12** (via app, confirmed 404) |
| **26-0160** | Pre-existing test cert; a **placeholder test PDF** was attached during the Edit repro (overwrote whatever it had) | Open — re-upload the intended PDF, or leave as-is |

Deletion performed with `Scripts/smoke-prod/delete-test-certs-live.js` (hardcoded allow-list of the two numbers; uses the app's real `POST /Certificate/Delete/{number}` endpoint).

Notes:
- All on **gccwebapp.com** (test env), not production.
- No CR required — pre-launch test records, not real business data ([[feedback_prelaunch_test_env]] / [[feedback_data_changes_require_cr]]).
- Cleanup method: delete via the app UI (Admin/Analyst) once logged in through the test gate.
- Localhost also has placeholder PDFs/thumbnails on cert **26-0023** from fix verification — disposable, no action needed.
