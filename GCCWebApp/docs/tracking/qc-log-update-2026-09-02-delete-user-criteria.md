# QC Log update — 2026-09-02 (in-cell, paste-ready) — Delete User not available: criteria

QC item (client): *"I was trying to cleanup the users and remove our test ones so I can create the
real team accounts. Sometimes I run into situation where Delete User is not an option. What criteria
is making this happen?"*

Client follow-up (8/27): *"I saw users were cleaned up. Can you just confirm the criteria that was in
place that was preventing deletion or was it just legacy?"*

> **⚠️ Fill in the QC item # and Reporter before pasting** — [[feedback_def_entries_note_qc_id]].

**Backing record:** none — **no DEF row and no CR.** Behavior is as-specified: "Admin user/role
cannot be deleted" is a standing project rule. This is a **close-out / working-as-designed** answer.

**Basis:** source review of `wwwroot/js/userManagement.js`, `Services/AdminService.cs`,
`Controllers/AdminController.cs` on 2026-09-02. Not re-exercised live on gccwebapp.com — the rule is
enforced in code on both the client and the server, and neither has changed.

---

## Status cell

```
Closed - working as designed. The only criteria is the account's Role being Admin. Admin accounts are protected from deletion by design; change the role to Analyst or External first and Delete User becomes available.
```

## Bucket / Root Cause cells

```
Bucket: Question
Root Cause: Working as designed - deliberate safeguard, not legacy. Admin accounts are blocked from deletion so the system can never be left without an administrator. Enforced in two places (the Actions menu and the server), and unchanged since the original build.
```

## Notes cell (paste as one cell; line breaks = Alt+Enter)

```
ANSWER - there is exactly ONE criteria: the account's Role is Admin.

Nothing else blocks deletion. Not certificates the user created, not their audit history, not company or partner assignment, not whether 2FA is enabled, not how old the account is, and not whether they have ever logged in. If Delete User is missing, that account is an Admin.

WHAT YOU SEE
- On an Admin account the Actions menu replaces "Delete User" with a greyed-out "Protected User" entry, tooltip "Admin users cannot be deleted."
- If the delete is attempted anyway, the server refuses it with "Cannot delete user. Admin users cannot be deleted or user not found."

IS IT LEGACY? No - it is a deliberate safeguard and it is still active.
It exists so the system can never be left with zero administrators, which would lock everyone out of user management, roles and settings with no way back in short of a database fix. The same protection covers the Admin ROLE on the Role Management page. It has been in the build since the original release and has not been changed.

The users that cleaned up successfully on 8/27 were the non-Admin ones. Any that still refuse are Admin accounts.

HOW TO REMOVE AN ADMIN TEST ACCOUNT
1. Actions menu > Edit User
2. Change Role from Admin to Analyst (or External) and save
3. Reopen the Actions menu - "Delete User" is now available

ONE CAUTION BEFORE THE CLEANUP
The protection is the role check alone - there is no separate "last administrator" safety net and no block on deleting your own account once it is no longer Admin. So please do not demote every Admin in order to clear them out. Keep your own account and the client Admin account on the Admin role at all times, which is what the go-live purge keep-list already assumes.

WHAT HAPPENS ON DELETE (useful while you build the real team accounts)
Deletion is a soft delete - the record is retained for audit and the deletion is written to the audit history with the account's details as they were. The email address and username are released at the same time, so you can immediately re-create a real team member on an address that a test account was using. Deleted accounts no longer appear in User Management and cannot sign in.
```

---

## Internal notes (do NOT paste into the QC log)

**Where the rule lives**

| Layer | File | Behavior |
|---|---|---|
| Actions menu render | `wwwroot/js/userManagement.js:396-408` | `user.role !== 'Admin'` → Delete User; else disabled "Protected User" |
| Client-side guard | `wwwroot/js/userManagement.js:625-629` | Blocks the call and toasts if invoked another way |
| Server | `Services/AdminService.cs:649-654` | `user.Roles.Any(r => r.Name == "Admin")` → `return false` |
| Server response | `Controllers/AdminController.cs:263` | "Cannot delete user. Admin users cannot be deleted or user not found." |
| Soft delete + slot release | `Services/AdminService.cs:663-673` | `IsDeleted`/`DeletedAt` set; email + username suffixed `__deleted__{id}__{timestamp}` because both columns carry unfiltered UNIQUE indexes |

Age of the rule: the Admin guard predates the audit-logging rework of `DeleteUserAsync` (commit
`9faecb6`) and is unchanged since — so "legacy?" answers as "deliberate, and still current."

**Considered and rejected as a defect.** The menu tests `Roles.FirstOrDefault()` while the server
tests `Roles.Any()`, so the two would disagree for a user holding more than one role with Admin not
first. Checked: every write path assigns exactly one role — Create (`AdminService.cs:174-178`),
Update (`AdminService.cs:549-556`, `Roles.Clear()` then add one), bulk import
(`AdminService.cs:354`) — and no seed script or SQL in the repo inserts a second `UserRoles` row.
The disagreement is unreachable, so it is not a requirement-vs-build gap. **No DEF raised**; noted
here only so it is not re-discovered and mis-triaged later.

**Gap worth watching (not raised).** There is no last-administrator guard and no self-delete guard —
the role check is the only thing preventing an empty Admin set, and it is bypassed by demoting an
account first. Not a defect against any current requirement; would be a CR if the client wants a
hard floor of one Admin. Flagged to the client as a caution in the Notes cell above.
