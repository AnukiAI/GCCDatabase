# docs-client

Client-facing deliverables and working notes that used to live **one level above the repo root**,
in the OneDrive folder `GCC Database Solution/`. A fresh clone got none of it. Moved in 2026-09-17.

Deliberately at the **repo root, not inside the `GCCWebApp/` project folder** — same reasoning as
`docs2/`: nothing here should ever be swept into a `dotnet publish`.

| Path | Contents |
|---|---|
| `qc-logs/` | Client QC log workbooks. Large binaries (~14 MB each). |
| `notes/` | Working analysis, pending-CR notes, the client letter, testing notes. |

Partner API documents did **not** come here — they joined their existing siblings in
`GCCWebApp/docs/tracking/api-samples/` rather than starting a second, competing home.

## What was deliberately left in OneDrive

- **`db-backups/`** (33 MB, 323 files) — production table dumps. `dbo.Users.jsonl` carries live
  bcrypt password hashes and email addresses; `dbo.ApiKeys.jsonl` and `dbo.PartnerApiKeys.jsonl`
  carry partner API keys, Amazon's included. **Never commit these.** They are also regenerable.
- **Nine superseded QC workbooks** (6.22.26 through 8.28.26, plus two `BACKUP-20260902` copies)
  — ~110 MB of frozen snapshots nobody diffs. `.xlsx` is zip-compressed, so git stores each one
  whole; there is no delta saving. Only the current workbooks are tracked, so git versions the
  log going forward instead of archiving its past.
- **Loose scratch** — `test_cookies.txt`, `test_login*.html`, `_v11.txt` (0 bytes) and the
  `test_import*.ps1` / `test_form_upload.*` one-offs. Same class of session junk the project
  `.gitignore` already excludes.

## A naming trap worth knowing

**The date in a QC workbook's filename is not its edit date.** As of 2026-09-17,
`Database QC Log_9.3.26_sw_updates.xlsx` had been modified more recently (2026-09-14) than either
`Database QC Log_9.11.26.xlsx` or `Database QC Log_9.9.26_sw_updates.xlsx` (both 2026-09-11).
Sort by modified time, not by the name, before citing one as current.
