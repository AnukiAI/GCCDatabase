# Client note — CSV export special-character issue (cert 26-9648 "Burt's Bees")

**Status:** Draft for Shawn to send. Investigation complete, fix applied to the test environment.
**Re:** CP note — *"Export database to csv — 26-9648 should have company name 'Burt's Bees' and instead exports as 'BurtÃ¢â‚¬â„¢s Bees'. Please check why certain characters are creating these extra symbols."*

---

Hi [name],

Thanks for flagging this — we looked into it right away. Good news: **your real data is fine, and the export software is working correctly.** Here's what we found.

**What caused the odd symbols.** The record you exported, **26-9648**, is one of our internal **test records** in the pre-launch test environment — not a real certificate. To make sure the system handles unusual characters correctly, we seed the test environment with a batch of deliberately tricky entries (curly apostrophes, accented letters, emoji, etc.). That particular batch was loaded into the test environment with a character-encoding fault baked in, so the apostrophe in the test copy of "Burt's Bees" was already stored incorrectly. The export then simply reproduced what was stored — the software didn't create the problem; it faithfully exported a record that was mis-loaded.

**Your genuine "Burt's Bees" record is correct.** We confirmed the real company record stores the apostrophe properly, and it exports cleanly. None of your actual certificates or company records are affected — the issue was confined to that batch of test-only entries.

**What we did.** We removed the affected test records from the test environment so they no longer appear in exports. No real data was touched.

**One thing worth knowing for the future.** When a CSV contains special characters (curly apostrophes, accents, ™/®, etc.), how it looks can depend on how you *open* it. Opening a CSV by double-clicking into some spreadsheet programs can misread those characters. If a real export ever looks off, opening it via the program's **"Import / From Text/CSV"** option (and choosing UTF-8) will display it correctly. We've also built in a safeguard on our side to make modern spreadsheets read the file correctly automatically.

If you ever see this on a **real** certificate (not a test record), please send it our way and we'll jump on it — but based on this review, your live data is clean.

Best,
[Shawn]

---

## Internal notes (do not send)

- Root cause: mojibake baked into a seeded QA "Test Edge Product" encoding battery in the test env (UTF-8 decoded as Windows-1252 at out-of-band load). App import/export code is UTF-8-clean; dev DB verified clean; the genuine Burt's Bees (Company Id 62) stores U+2019 correctly. The client's export showed *double* mojibake because their spreadsheet opened the CSV as Windows-1252.
- Action: deleted the corrupt fixtures only — 5 test companies (incl. duplicate Burt's Id 288), 47 products, 47 certs (incl. 26-9648), 752 cascaded product-IDs. Kept ~130 clean Test Edge fixtures. Detection sweep re-run = 0 corrupt rows.
- Scripts: `Scripts/data-ops/2026-07-17-mojibake-encoding-detection-IDENTIFY.sql`, `Scripts/data-ops/2026-07-19-delete-corrupt-encoding-fixtures-REMEDIATE.sql`. Backup: `Scripts/data-ops/_backups/2026-07-19-corrupt-encoding-fixtures/deleted-rows-backup.txt`.
- Classification: data issue, not a defect (no DEF row) — export code was already correct. The "safeguard" mentioned to the client is the UTF-8 BOM export helper (`Extensions/CsvExportHelper.cs`); confirm it is deployed to the test env.
