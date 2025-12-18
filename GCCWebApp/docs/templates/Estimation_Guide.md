# Project Estimation Guide

**Purpose:** Convert complexity scores into realistic project estimates. Based on lessons learned from GCC Database Solution and industry benchmarks.

---

## 1. Complexity-to-Hours Baseline

### 1.1 Base Estimates

| Complexity Score | Category | Base Hours | Calendar Time (1 dev) |
|------------------|----------|------------|----------------------|
| 0-30 | Tiny | 20-40 | 1-2 weeks |
| 31-50 | Small | 40-60 | 2-3 weeks |
| 51-75 | Small-Medium | 60-100 | 3-5 weeks |
| 76-150 | Moderate | 100-200 | 1-3 months |
| 151-300 | Complex | 200-500 | 3-6 months |
| 301-500 | Very Complex | 500-1000 | 6-12 months |
| 500+ | Program | 1000+ | 12+ months / phased |

### 1.2 Hours per Complexity Point

| Score Range | Hours per Point |
|-------------|-----------------|
| 0-75 | ~1.0-1.3 hours |
| 76-150 | ~1.3-1.5 hours |
| 151-300 | ~1.5-1.8 hours |
| 301-500 | ~1.8-2.0 hours |
| 500+ | ~2.0+ hours |

**Why does it scale?** Larger projects have:
- More integration points
- More edge cases
- More coordination overhead
- More testing complexity
- More documentation needs

---

## 2. Adjustment Factors

Apply these multipliers to base estimate:

### 2.1 Team Factors

| Factor | Multiplier | When to Apply |
|--------|------------|---------------|
| Team very experienced with stack | 0.8x | 3+ similar projects completed |
| Team moderately experienced | 1.0x | 1-2 similar projects |
| Team new to stack | 1.3x | First project with this tech |
| Solo developer | 1.0x | Baseline |
| 2-person team | 0.7x | Good pairing, less communication overhead |
| 3+ person team | 0.8-1.0x | Coordination overhead increases |
| Remote team, different time zones | 1.2x | Communication delays |

### 2.2 Client Factors

| Factor | Multiplier | When to Apply |
|--------|------------|---------------|
| Client very responsive (< 24hr) | 0.9x | Quick decisions, fast feedback |
| Client moderately responsive | 1.0x | Baseline |
| Client slow (3-5 day delays) | 1.2x | Delays compound |
| Client very slow (1+ week) | 1.5x | Consider re-scoping |
| Clear requirements, signed off | 0.9x | Low ambiguity |
| Moderate ambiguity | 1.0x | Baseline |
| High ambiguity, "figure it out" | 1.3x | Discovery during development |
| Multiple stakeholders | 1.1x | Conflicting opinions |
| Single decision-maker | 0.95x | Faster approvals |

### 2.3 Technical Factors

| Factor | Multiplier | When to Apply |
|--------|------------|---------------|
| Greenfield (new project) | 1.0x | Baseline |
| Adding to existing codebase | 1.1x | Learning curve, integration |
| Legacy codebase, poor quality | 1.4x | Technical debt |
| Third-party API integrations | +10-20hrs each | API learning, error handling |
| Complex security requirements | 1.2x | HIPAA, PCI, etc. |
| High performance requirements | 1.2x | Optimization time |
| Mobile + Web | 1.5-2.0x | Two platforms |

### 2.4 Process Factors

| Factor | Multiplier | When to Apply |
|--------|------------|---------------|
| Requirements fully documented | 0.9x | This questionnaire completed |
| Requirements partially documented | 1.0x | Baseline |
| Requirements vague/verbal only | 1.4x | Discovery during development |
| Design mockups provided | 0.9x | Less design iteration |
| No design direction | 1.2x | Design iteration cycles |
| Formal testing/QA process | 1.1x | Time for test cycles |
| Informal testing | 1.0x | Baseline |

---

## 3. Estimation Formula

```
Final Estimate = Base Hours × Team Factor × Client Factor × Technical Factor × Process Factor × Contingency

Where:
- Base Hours = Complexity Score × Hours-per-Point
- Contingency = 1.2 (simple) to 1.4 (complex)
```

### 3.1 Example: GCC Project

```
Complexity Score: 245 (Complex category)
Base Hours: 245 × 1.6 = 392 hours

Adjustments:
- Team moderately experienced with .NET: 1.0x
- Client moderately responsive: 1.0x
- Greenfield project: 1.0x
- Requirements partially documented: 1.0x (should have been 0.9x with full discovery)
- No design mockups: 1.2x

Adjusted: 392 × 1.0 × 1.0 × 1.0 × 1.0 × 1.2 = 470 hours

Contingency (complex): 1.3x
Final: 470 × 1.3 = 611 hours

Actual result: ~600-700 hours over 6 months
```

**With proper discovery (requirements factor 0.9x, design factor 0.9x):**
```
392 × 0.9 × 0.9 × 1.3 = 413 hours (~4 months)
```

---

## 4. Component-Based Estimation

Break down by component for more accuracy:

### 4.1 Per-Component Hours

| Component | Small | Medium | Complex |
|-----------|-------|--------|---------|
| **Data Model** | | | |
| Simple entity (5-10 fields) | 2-4 | | |
| Medium entity (10-20 fields) | | 4-8 | |
| Complex entity (20+ fields, relationships) | | | 8-16 |
| N:N relationship with junction table | | 4-6 | |
| **Views** | | | |
| Simple list (table, basic pagination) | 4-8 | | |
| Medium list (filters, search, sorting) | | 12-20 | |
| Complex list (infinite scroll, autocomplete, URL state) | | | 24-40 |
| Simple form (5-10 fields) | 4-6 | | |
| Medium form (10-20 fields, validation) | | 8-16 | |
| Complex form (conditional, multi-step, file upload) | | | 20-40 |
| Dashboard with charts | | 16-24 | |
| **Features** | | | |
| Basic auth (login, logout, password reset) | | 16-24 | |
| 2FA | | | 20-30 |
| Role-based permissions | | 8-16 | |
| Complex data visibility rules | | | 16-32 |
| CSV export | 4-6 | | |
| CSV import with validation | | 12-20 | |
| Bulk operations | | 8-12 | |
| File upload (single) | 4-6 | | |
| File upload (multiple, drag-drop, progress) | | 12-16 | |
| Email notifications (per template) | 2-4 | | |
| REST API (per endpoint group) | | 8-16 | |
| Third-party API integration | | 16-32 | |
| Workflow/state machine (simple: 3-5 states) | | 12-20 | |
| Workflow (complex: 10+ states) | | | 30-50 |
| **Testing** | | | |
| Unit tests (per component) | 2-4 | | |
| Integration tests (per feature) | | 4-8 | |
| E2E tests (per flow) | | 8-12 | |
| **Other** | | | |
| Deployment setup | 4-8 | | |
| Documentation (user) | | 8-16 | |
| Documentation (technical) | | 8-16 | |

### 4.2 Example Breakdown: GCC Project

| Component | Estimate | Actual |
|-----------|----------|--------|
| Data Model (9 entities, complex relationships) | 60 | 70 |
| Certificate Tile Views (3 views, complex) | 80 | 120* |
| Certificate Create/Edit Form | 30 | 40 |
| Admin Pages (User, Role, Company, Product, Tag) | 60 | 80 |
| Dashboard | 20 | 24 |
| Authentication + 2FA | 40 | 45 |
| Role Permissions + Data Visibility | 30 | 50* |
| Workflow (14 states) | 40 | 50 |
| Import/Export | 30 | 35 |
| Partner API | 20 | 30 |
| Email Notifications | 16 | 20 |
| Testing | 80 | 90 |
| Deployment | 16 | 20 |
| **TOTAL** | **522** | **654** |

*Items with asterisk had scope creep that proper discovery would have caught.

---

## 5. Contingency Guidelines

| Project Type | Contingency | Rationale |
|--------------|-------------|-----------|
| Repeat project (done similar before) | 15% | Known unknowns |
| New but well-defined | 20% | Some unknowns |
| New with moderate ambiguity | 30% | Unknown unknowns |
| New technology + ambiguous requirements | 40% | High risk |
| Client has history of scope creep | 40-50% | Protect yourself |

### 5.1 Where Contingency Gets Used

```
Typical contingency breakdown:
- Bug fixes: 20-30%
- Requirement clarifications: 20-30%
- Scope creep (even with sign-off): 15-25%
- Technical surprises: 15-20%
- Integration issues: 10-15%
```

---

## 6. Pricing Strategies

### 6.1 Pricing Models

| Model | When to Use | Risk |
|-------|-------------|------|
| Fixed price | Well-defined scope, experienced team | Developer risk |
| Time & materials | Ambiguous scope, discovery work | Client risk |
| Capped T&M | Moderate ambiguity, shared risk | Balanced |
| Phased fixed | Large projects, milestones | Balanced |
| Retainer | Ongoing work, unpredictable volume | Balanced |

### 6.2 Fixed Price Formula

```
Fixed Price = (Estimated Hours × Hourly Rate) + Contingency Buffer

Where:
- Contingency Buffer = 20-40% depending on risk
- Never quote exactly your estimate
```

### 6.3 Presenting Estimates

**DO:**
```
"Based on our discovery sessions, I estimate 400-500 hours
for the core features we discussed. This includes a 25%
contingency for the unknowns we always encounter.

Phase 1 (MVP): 300-350 hours
Phase 2 (Nice-to-have): 100-150 hours

I recommend starting with Phase 1 and re-evaluating Phase 2
after launch."
```

**DON'T:**
```
"It'll be about 400 hours."
(No range, no contingency, no phasing = recipe for conflict)
```

---

## 7. Tracking Actuals

### 7.1 Track During Project

| Week | Estimated | Actual | Variance | Notes |
|------|-----------|--------|----------|-------|
| 1 | 40 | 45 | +5 | Setup took longer |
| 2 | 40 | 38 | -2 | |
| 3 | 40 | 52 | +12 | Scope creep: filter changes |
| ... | | | | |
| Total | 400 | 450 | +50 | |

### 7.2 Post-Project Analysis

After each project, document:

```
PROJECT RETROSPECTIVE: [Name]

Estimated: _____ hours
Actual: _____ hours
Variance: _____ hours (___%)

Top 3 reasons for variance:
1. _______________________________
2. _______________________________
3. _______________________________

What would I estimate next time? _____ hours

Lessons for future estimates:
- _______________________________
- _______________________________
```

---

## 8. Quick Reference Tables

### 8.1 Feature Sizing Cheat Sheet

| Feature | Typical Hours |
|---------|---------------|
| Login page | 8-12 |
| User registration | 12-16 |
| Password reset | 8-12 |
| 2FA setup | 16-24 |
| Simple CRUD page | 16-24 |
| Complex list with filters | 24-40 |
| Dashboard with 4-6 widgets | 24-32 |
| CSV export | 4-8 |
| CSV import with validation | 16-24 |
| REST API (CRUD for one entity) | 16-24 |
| Email notification (per template) | 2-4 |
| File upload feature | 8-16 |
| Search with autocomplete | 16-24 |
| Role-based permissions | 16-24 |
| Simple workflow (3-5 states) | 16-24 |
| Complex workflow (10+ states) | 40-60 |

### 8.2 Red Flags That Blow Estimates

| Red Flag | Typical Impact |
|----------|----------------|
| "Just like [big product]" | 2-3x estimate |
| "It's simple" (from client) | 1.5x estimate |
| No mockups or wireframes | +20-30% |
| Multiple decision-makers | +15-20% |
| Vague requirements | +30-50% |
| Client's first software project | +20-30% |
| Integrating with legacy system | +30-50% |
| "We'll figure it out as we go" | +40-60% |
| Aggressive timeline | Quality suffers OR +20-30% |

---

## 9. GCC Project: Estimation Post-Mortem

### What We Would Estimate Now

| Component | Original Discovery | With Proper Discovery |
|-----------|-------------------|----------------------|
| Views | "3 basic views" → 40 hrs | "3 complex views with infinite scroll, autocomplete, URL state, bulk actions" → 100 hrs |
| Filters | "Some filters" → 10 hrs | "Database-driven filters, multi-select, URL state across 3 views" → 35 hrs |
| Search | "Search box" → 8 hrs | "Autocomplete with categorized suggestions, debounce, URL state" → 25 hrs |
| Data visibility | "External sees their data" → 10 hrs | "Complex joins: User ↔ Company ↔ Certificate, User ↔ Partner ↔ Certificate" → 35 hrs |
| Product IDs | "Link products" → 8 hrs | "N:N with 7 types (ASIN, UPC, etc.), bulk CSV import, 100s per certificate" → 30 hrs |

**Lesson:** Generic phrases hide complexity. Force specifics during discovery.

---

*Use this guide alongside the Requirements Questionnaire to produce accurate estimates.*
