# GCC Database Solution — Amazon API Integration Timeline
**Prepared by:** Anuki Consulting
**Date:** April 15, 2026
**Prepared for:** GreenCircle Certified

---

## Executive Summary

Amazon has requested firm milestone dates for API integration and automated badging. This document presents two timeline options with dependencies, risk factors, and mitigation strategies. Both options are achievable contingent on completing the current UAT cycle without significant new scope.

---

## Current State of Readiness

| Component | Status | Notes |
|-----------|--------|-------|
| Certificate Database Application | 95% complete | 40 items in final UAT retest |
| REST API Endpoints | Built | `/api/v1/certificates`, search by ASIN, GS1 JSON-LD format |
| API Authentication | Built | Partner API keys, rate limiting (200 req/hr) |
| Amazon Developer Guide | CP2 Review Complete | Reviewed by GCC, feedback incorporated |
| GS1 Data Schema | Implemented | JSON-LD format per GS1 Web Vocabulary standard |
| Amazon SP-API Sync (Push) | Not Started | Credential storage built; sync logic pending |
| Automated Badging Connection | Not Started | Dependent on API completion and Amazon coordination |

---

## Option A — Aggressive Timeline (Amazon's Requested Dates)

| Phase | Milestone | Date | Duration |
|-------|-----------|------|----------|
| **Phase 1** | Database Stabilization & UAT Retest | Apr 15 — May 15 | 4 weeks |
| **Phase 2** | API Schema Finalization & Lock | May 16 — May 31 | 2 weeks |
| **Phase 3** | Amazon SP-API Implementation | Jun 1 — Jul 15 | 6 weeks |
| **Phase 4** | Integration Testing & QA | Jul 16 — Jul 31 | 2 weeks |
| **Phase 5** | Automated Badging Go-Live | Aug 1 — Aug 31 | Rollout |

### Phase 1: Database Stabilization & UAT Retest (Apr 15 — May 15)
**Objective:** Error-free database application ready for production use

Activities:
- Deploy current code fixes (24 modified files) and 4 SQL migration scripts
- Client UAT retest of 40+ resolved items
- Address any retest failures
- Resolve 5 items pending client decision (Partner role, Duo MFA, forgot password, ASIN scaling, cert type filters)
- Data cleanup (test entries, product hierarchy corrections)

**Exit Criteria:** All critical/high QC items confirmed fixed. No open blockers.

### Phase 2: API Schema Finalization & Lock (May 16 — May 31)
**Objective:** Finalized API endpoint and data schema per Amazon's Milestone 1

Activities:
- Confirm GS1 JSON-LD schema meets Amazon's CPF requirements
- Document final field mappings (ASIN-only per Amazon; no UPC/GTIN/EAN)
- Lock API contract — no breaking changes after this date
- Publish final API documentation for Amazon technical team
- Production deployment of stabilized database

**Deliverable:** Finalized API specification and data schema documentation
**Amazon Milestone 1: May 31, 2026**

### Phase 3: Amazon SP-API Implementation (Jun 1 — Jul 15)
**Objective:** Build automated certificate data sync with Amazon systems

Activities:
- Implement Amazon SP-API credential management and authentication
- Build automated certificate push/sync logic
- Implement partner-scoped data filtering (Amazon sees only Amazon-partnered certs)
- Build webhook notifications for certificate status changes
- Error handling, retry logic, and sync failure recovery
- Unit and integration testing against Amazon sandbox

**Deliverable:** Functional API integration with Amazon SP-API

### Phase 4: Integration Testing & QA (Jul 16 — Jul 31)
**Objective:** Complete API technical implementation and testing per Amazon's Milestone 2

Activities:
- End-to-end testing with Amazon sandbox environment
- Load testing and performance validation
- Security review of API authentication and data handling
- UAT with GCC team on API functionality
- Documentation updates and runbook for operations

**Deliverable:** Tested, production-ready API integration
**Amazon Milestone 2: July 31, 2026**

### Phase 5: Automated Badging Go-Live (Aug 2026)
**Objective:** Begin automated badging via API connection per Amazon's Milestone 3

Activities:
- Production deployment of Amazon SP-API integration
- Coordinate go-live with Amazon technical team
- Monitor initial automated sync cycles
- Address any production issues
- Transition to steady-state operations

**Amazon Milestone 3: August 2026**

---

## Option B — Buffered Timeline (+1 Month Per Milestone)

| Phase | Milestone | Date | Duration |
|-------|-----------|------|----------|
| **Phase 1** | Database Stabilization & UAT Retest | Apr 15 — May 31 | 6 weeks |
| **Phase 2** | API Schema Finalization & Lock | Jun 1 — Jun 30 | 4 weeks |
| **Phase 3** | Amazon SP-API Implementation | Jul 1 — Aug 15 | 6 weeks |
| **Phase 4** | Integration Testing & QA | Aug 16 — Aug 31 | 2 weeks |
| **Phase 5** | Automated Badging Go-Live | Sep 1 — Sep 30 | Rollout |

### Phase 1: Database Stabilization & UAT Retest (Apr 15 — May 31)
**Objective:** Same as Option A with additional buffer for retest cycles

Additional time accounts for:
- Second round of retest failures (historically, each UAT cycle generates 15-30 new items)
- Client availability for testing (GCC staff have competing priorities)
- Resolution of discussion items requiring client decisions
- Production environment configuration and DNS cutover

**Exit Criteria:** All QC items confirmed fixed. Production environment stable.

### Phase 2: API Schema Finalization & Lock (Jun 1 — Jun 30)
**Objective:** Finalized API endpoint and data schema

Additional time accounts for:
- Schema review cycle with Amazon technical team (may require iterations)
- Potential field additions based on Amazon's CPF requirements review
- GCC internal review of data being exposed via API
- Production database fully populated with real certificate data

**Amazon Milestone 1 (Buffered): June 30, 2026**

### Phase 3: Amazon SP-API Implementation (Jul 1 — Aug 15)
**Objective:** Build and test automated certificate data sync

Same scope as Option A Phase 3.

### Phase 4: Integration Testing & QA (Aug 16 — Aug 31)
**Objective:** Complete API technical implementation and testing

Same scope as Option A Phase 4.

**Amazon Milestone 2 (Buffered): August 31, 2026**

### Phase 5: Automated Badging Go-Live (Sep 2026)
**Objective:** Begin automated badging via API connection

Same scope as Option A Phase 5.

**Amazon Milestone 3 (Buffered): September 2026**

---

## Side-by-Side Comparison

| Milestone | Option A (Aggressive) | Option B (Buffered) | Delta |
|-----------|----------------------|---------------------|-------|
| API Schema Finalized | May 31, 2026 | June 30, 2026 | +1 month |
| API Implementation & Testing Complete | July 31, 2026 | August 31, 2026 | +1 month |
| Automated Badging Go-Live | August 2026 | September 2026 | +1 month |
| UAT Buffer | 4 weeks | 6 weeks | +2 weeks |
| API Dev Time | 6 weeks | 6 weeks | Same |
| Testing Time | 2 weeks | 2 weeks | Same |

---

## Schedule Risk Factors

### High Impact Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| **UAT retest generates significant new defects** | Delays Phase 1 exit, cascades to all milestones | Medium-High (historically each UAT cycle produces 15-30 new items) | Feature freeze after Phase 1. Only bug fixes, no new scope. |
| **New feature requests during UAT** | Scope creep delays stabilization | Medium (has happened repeatedly in this project) | All new requests after UAT start treated as formal Change Requests with separate timeline. |
| **Amazon schema requirements change after review** | Rework on finalized API | Medium | Early schema review with Amazon technical team. Lock schema with formal sign-off. |
| **Client availability for testing** | UAT cycle extends | Medium | Provide detailed test cases with steps. Set testing windows with committed dates. |

### Medium Impact Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| **Amazon sandbox access delays** | Blocks integration testing | Low-Medium | Request sandbox credentials by May 15 regardless of timeline option. |
| **Azure AD B2C configuration issues** | Blocks forgot password, Duo MFA | Low | These are not on the Amazon critical path. Can be addressed in parallel. |
| **Production environment DNS/hosting changes** | Deployment disruption | Low | URL structure already confirmed (greencirclecertified.com/database). DNS change is pending but independent of API work. |
| **Partner role implementation (QC 143)** | Amazon can see non-Amazon certs via API | Medium | Current API already filters by partner. Full role-based scoping is enhancement, not blocker. |

### Dependencies (External to Anuki)

| Dependency | Owner | Needed By | Status |
|------------|-------|-----------|--------|
| Complete UAT retest of 40 open items | GCC (Carly) | May 15 (Option A) / May 31 (Option B) | Not started |
| Decisions on 5 discussion items | GCC (Carly/Dan) | May 15 | Pending |
| Amazon SP-API sandbox credentials | GCC / Amazon | Jun 1 | Not requested |
| Amazon technical team schema review | Amazon | Jun 15 | Not scheduled |
| Final production URL / DNS configuration | GCC | Jul 1 | Pending |
| Real certificate data loaded in production DB | GCC (Analysts) | Jul 1 | Partial |

---

## Recommendation

**Anuki recommends Option B (Buffered Timeline)** for the following reasons:

1. **Historical pattern:** Every UAT cycle on this project has generated 15-30 new items. Committing to Option A assumes a clean retest with zero new findings, which has not occurred in any prior cycle.

2. **Development time is the same:** Both options allocate 6 weeks for SP-API implementation and 2 weeks for testing. The buffer is entirely in the UAT/stabilization phase where risk is highest.

3. **Accountability protection:** If Amazon holds GCC to these dates, a missed aggressive deadline damages the relationship more than a met conservative one.

4. **End of Q3 alignment:** Option B completes by end of Q3 2026, which is a natural reporting boundary.

However, if Amazon requires the aggressive timeline, **Option A is achievable** provided:
- GCC commits to completing UAT retest by May 15 with dedicated testing time
- No new feature requests are introduced during the stabilization phase
- All 5 pending discussion items are resolved by May 1

---

## Immediate Next Steps

| Action | Owner | Due |
|--------|-------|-----|
| Deploy current code fixes + SQL scripts to production | Anuki | Apr 16 |
| Begin UAT retest using provided test cases (40 items) | GCC | Apr 17 |
| Resolve 5 pending discussion items | GCC + Anuki | Apr 30 |
| Confirm timeline option with Amazon | GCC | Apr 15 |
| Request Amazon SP-API sandbox credentials | GCC | May 1 |
| Complete UAT retest cycle | GCC | May 15 / May 31 |

---

*Prepared by Anuki Consulting for GreenCircle Certified. Confidential.*
