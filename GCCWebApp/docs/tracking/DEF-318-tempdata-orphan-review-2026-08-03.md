# Review — "Certificate created" popup appearing out of context

**QC #193 · Image 106 · DEF-318 (fixed, undeployed) + DEF-330 (fixed today) + DEF-331 (open)**
**Date:** 2026-08-03 · **Prepared by:** Anuki · **Status:** for Shawn's review — nothing committed, nothing deployed

---

## 1. What the client is seeing

> *"Go to existing cert, Edit and change status (or any other field), Save, navigate to Certificates in header, click Add Certificate button. Then box appears about successful add — this is out of context."* — client, 7/30

Two things made this look mysterious: it fires **after the page has already loaded**, and it **survives a logout/login**. Both are explained by the same mechanism.

---

## 2. Root cause — an orphaned `TempData` write, not a popup bug

ASP.NET Core does **not** discard an *unread* `TempData` entry. It re-saves it into the cookie on every subsequent request until something reads that key. "Read-once" only starts counting **after** the first read.

1. `Edit` (POST) set `TempData["SuccessMessage"]`, then redirected to `CertificateInternalView`.
2. `CertificateInternalView.cshtml` **reads no TempData at all** — nothing consumed it.
3. The entry rode along in the cookie indefinitely (hence surviving logout/login).
4. The next view that *does* read that key was `Views/Certificate/Create.cshtml` — so clicking **Add Certificate**, at any later point, popped the old edit's message onto a blank form.

The 6/23 "resolved" (DEF-291) was a genuine but **different** fix — a bfcache/Back-button path — verified only against *create → save & add another*. The client's path was never exercised.

**The rule that came out of it:**

> Write `TempData` only when the redirect target itself reads that key. A key must never have a writer without a reader, or a reader without a writer.

---

## 3. What DEF-318 changed (already done 7/31 — in working tree, **not deployed**)

Two techniques:

**A. Delete the orphaned write** — `CertificateController.cs`, Edit POST

```diff
-  TempData["SuccessMessage"] = $"Certificate {certificate.Number} updated successfully!";
   return RedirectToAction(nameof(CertificateInternalView), new { id = certificate.Number });
```

Nothing was lost — nothing ever rendered it. No reader was added to the internal view; that would be new UI the client did not ask for.

**B. Give the surviving flow a private key** — `CertificateController.cs:704` + `Create.cshtml:48`

```diff
-  TempData["SuccessMessage"]       = $"Certificate {certificate.Number} created successfully!";
+  TempData["CreateSuccessMessage"] = $"Certificate {certificate.Number} created successfully!";
```

One writer, one reader, and the redirect target **is** the reader — consumed on the very next request. No foreign message can reach that form again by construction.

**C.** An invariant comment in `CertificateInternalView.cshtml` so the next person does not re-derive it.

> ⚠️ **This fix is uncommitted.** `HEAD` still contains 2 `TempData["SuccessMessage"]` writes in `CertificateController.cs`; the working tree has 0. That is why the client is still seeing it on gccwebapp.com. **It needs no further work — only the next deploy bundle.**

---

## 4. What was found today, and fixed (DEF-330)

DEF-318 fixed the reported path but never swept for the same shape elsewhere. A full writer/reader audit of every `TempData` key found **three more**:

| # | Writer | Redirect target | Reader there? | Where it actually surfaced |
|---|---|---|---|---|
| 1 | `CertificateImportController.cs:174` — *"Import completed successfully! N created, M updated."* | `Certificate/CertificateTileView` | **No** | Later, on Import Index/Preview/History — an out-of-context *"N certificates created"* banner |
| 2 | `DocumentController.cs:87` — *"Document uploaded successfully."* | `Certificate/Details` (**doesn't exist** — §5) | **No** | Later, on the **2FA settings page** |
| 3 | `CertificateImportController.cs:187` — `TempData["ErrorDetails"]` | *(same-request `View("Preview")`)* | **No reader anywhere** | Never shown at all; parked a multi-KB string in the cookie indefinitely |

**#1 is the near-twin of the client's report** — literally an *"N certificates created"* message reappearing on an unrelated page load.

**#3 had no visible symptom but the worst failure mode.** An unread multi-KB entry re-saved on every request is a cookie-size problem, and a large enough cookie is a 400 Bad Request.

### Fix applied — all three writes deleted, **no banner added anywhere**

Per your direction today (*"I don't want a banner at the top"*), the like-for-like fix is deletion, not relocation:

| # | Action | Anything lost? |
|---|---|---|
| 1 | Write removed; redirect unchanged | **No** — it was never rendered on the tile view in the first place |
| 2 | Write removed; broken redirect left alone and logged as DEF-331 | **No** — never rendered |
| 3 | Write removed | **No** — the same per-row errors already render on that page from `Model.ErrorRows` (`Preview.cshtml:216-226`) |

Each deletion carries an inline comment naming DEF-330 and restating the invariant, matching the convention DEF-318 set.

### Audit result after the fix

Every remaining `TempData` key is paired:

| Key | Writers | Reads | Verdict |
|---|---|---|---|
| `CreateSuccessMessage` | 1 | 1 | ✅ target is the reader |
| `SuccessMessage` | 3 (all `AccountController`) | 1 | ✅ all 3 → `TwoFactorPreferences`, which reads it |
| `Success` | 1 | 6 | ✅ → `History`, which reads it |
| `Error` / `Info` / `ErrorMessage` / auth keys | paired | paired | ✅ |
| `ErrorDetails` | **0** | 0 | ✅ removed |

**Build:** `dotnet msbuild -t:Compile` → **0 errors**. (The 2 warnings are file-copy locks from the app running on :5101, not compile diagnostics.)

---

## 5. Also found — logged, deliberately NOT fixed (DEF-331)

`DocumentController.cs:88` redirects a **successful** upload to `Certificate/Details`. There is no `Details` action on `CertificateController` and no `Views/Certificate/Details.cshtml`. **That redirect 404s.**

Not fixed because choosing the replacement destination is a behaviour decision, not a leak repair — and the likely target (`CertificateInternalView`) keys on certificate **Number**, not **Id**, so it is not a one-word substitution.

**Before it is worth any effort:** is this controller still reachable from the UI, or has document upload been superseded by the Create/Edit upload flow? If it is dead code, the right fix is removal, not a new redirect.

---

## 6. Decisions needed from you

| # | Item | Recommendation |
|---|---|---|
| 1 | **DEF-318 + DEF-330 → next deploy bundle.** Both are code-complete in the working tree. | Ship. This is what closes QC #193 for the client. |
| 2 | ~~**Import confirmation gap.**~~ **DECIDED 2026-08-04 — Shawn: *"the user should still get the confirmation."*** Closed by **DEF-338**: the write is back under a private key (`ImportSuccessMessage`) whose one reader is the redirect target itself, rendered the same way the Create flow renders its confirmation. No banner. Third option neither of us listed on 8/3 — and it is the one DEF-318 had already proven. | Done; ships in the same bundle. |
| 3 | **DEF-331 reachability check** (~0.5 h) before deciding fix-vs-remove. | Cheap; worth doing before the next bundle so a successful upload isn't landing anyone on a 404. |

---

## 7. Files touched today

```
Controllers/CertificateImportController.cs   2 orphan writes removed (+ comments)
Controllers/DocumentController.cs            1 orphan write removed (+ comment, + DEF-331 note)
docs/tracking/defect-register.md             DEF-330, DEF-331 added
observations_log.md                          appended
```

Nothing committed — per standing practice, the working tree is left for you to commit.
