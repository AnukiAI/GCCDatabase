# Issue Register

**Project:** GCC Database Solution
**Created:** 2026-03-23
**Last Updated:** 2026-03-23

## Status Definitions

| Status | Meaning |
|--------|---------|
| Open | Identified, not yet investigated |
| Investigating | Under analysis |
| In-Progress | Actively being resolved |
| Resolved | Fix applied |
| Closed | Confirmed resolved — no further action |

## Category Definitions

| Category | Meaning |
|----------|---------|
| Technical | Infrastructure, architecture, or tooling problem |
| Operational | Process, workflow, or day-to-day operations problem |
| Environment | Dev/staging/prod environment configuration issue |
| Process | Methodology, procedure, or team process gap |

---

## Issue Summary

| Priority | Open | Investigating | In-Progress | Resolved | Closed | Total |
|----------|------|---------------|-------------|----------|--------|-------|
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |
| High | 1 | 0 | 0 | 0 | 0 | 1 |
| Medium | 1 | 0 | 0 | 0 | 0 | 1 |
| Low | 0 | 0 | 0 | 0 | 0 | 0 |
| **Total** | **2** | **0** | **0** | **0** | **0** | **2** |

---

## Issue Log

### ISS-001 — Test suites do not cover user journeys, use cases, or end-to-end flows

| Field | Value |
|-------|-------|
| Category | Process |
| Reported By | Steve |
| Reported Date | 2026-03-23 |
| Priority | High |
| Status | Open |
| Assigned To | Unassigned |
| Impact | Defects reach UAT that should be caught earlier. Testing burden falls entirely on one person. Edge cases and negative scenarios not covered. |
| Related DEF/CR | Multiple DEFs from QC log |
| Resolution | — |
| Resolved Date | — |

> **Note:** Current test suites lack user journey coverage, use case validation, true E2E flows, edge case testing, and negative testing. This gap contributed to 80+ defects reaching client UAT.

---

### ISS-002 — UAT testing capacity — single tester for full application

| Field | Value |
|-------|-------|
| Category | Operational |
| Reported By | Steve |
| Reported Date | 2026-03-23 |
| Priority | Medium |
| Status | Open |
| Assigned To | Steve |
| Impact | Testing bottleneck. One person cannot adequately cover all user roles, flows, browsers, and devices. Risk of defects reaching production. |
| Related DEF/CR | — |
| Resolution | — |
| Resolved Date | — |

---

## Template — Copy for New Issues

```
### ISS-XXX — [Title]

| Field | Value |
|-------|-------|
| Category | [Technical/Operational/Environment/Process] |
| Reported By | [Name] |
| Reported Date | YYYY-MM-DD |
| Priority | [Critical/High/Medium/Low] |
| Status | Open |
| Assigned To | Unassigned |
| Impact | [What is blocked or impaired] |
| Related DEF/CR | — |
| Resolution | — |
| Resolved Date | — |
```
