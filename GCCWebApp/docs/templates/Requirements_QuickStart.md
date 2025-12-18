# Requirements Quick-Start Guide

**For projects under 75 complexity points (~40-80 hours)**

Use this condensed version for small features, simple CRUD apps, or MVPs. For larger projects, use the full Requirements Questionnaire.

---

## Pre-Call (15 minutes)

Send to client before the call:

```
Please answer before our call:
1. In one sentence, what problem does this solve?
2. Who will use it? (List user types)
3. Is there a hard deadline? Why?
4. Budget range?
5. Show me 1-2 apps/sites you like the look of
```

---

## Discovery Call (60-90 minutes)

### Part 1: Foundation (15 min)

| Question | Answer |
|----------|--------|
| What problem are we solving? | |
| What does success look like? | |
| Who makes final decisions? | |
| Hard deadline? | |
| Budget? | |

### Part 2: Users (10 min)

| Question | Answer |
|----------|--------|
| User types/roles? | |
| What can each role do? | |
| How do users log in? | |
| See only own data, or all data? | |

### Part 3: Data (15 min)

| Question | Answer |
|----------|--------|
| Main things to track? (entities) | |
| For each: what fields? | |
| How do they relate? (1:1, 1:N, N:N) | |
| Any status/workflow? | |

### Part 4: Screens (20 min)

**For each screen:**

| Screen Name | Fields Shown | Filters | Search | Actions | Notes |
|-------------|--------------|---------|--------|---------|-------|
| | | | | | |
| | | | | | |
| | | | | | |

**Quick questions per screen:**
- Table or cards?
- Pagination or infinite scroll?
- What filters? (list them)
- Search? What fields?
- Bulk actions needed?
- Mobile layout same or different?

### Part 5: Forms (10 min)

**For each form:**

| Form Name | Fields | Required | Validation | Notes |
|-----------|--------|----------|------------|-------|
| | | | | |
| | | | | |

### Part 6: Quick Hits (10 min)

| Feature | Yes/No | Details |
|---------|--------|---------|
| File uploads? | | |
| Email notifications? | | |
| Export to CSV/Excel? | | |
| Import from CSV? | | |
| API needed? | | |
| Mobile app or web only? | | |

---

## After Call (30 min)

1. Write up requirements
2. Calculate quick complexity score
3. Send estimate
4. Get sign-off before starting

---

## Quick Complexity Calculator

Count points:

| Item | Points |
|------|--------|
| Each user role | +2 |
| Each entity | +2 |
| Each view/page | +3 |
| Each form | +2 |
| Each filter | +1 |
| Each bulk action | +3 |
| File uploads | +4 |
| Email notifications | +3 |
| CSV export | +2 |
| CSV import | +5 |
| API | +8 |
| Mobile-specific layout | +5 |
| Search with autocomplete | +5 |
| Workflow/status | +5 |
| Multi-role permissions | +5 |

**Total: ______ points**

| Score | Size | Estimate |
|-------|------|----------|
| 0-30 | Tiny | 20-40 hrs |
| 31-50 | Small | 40-60 hrs |
| 51-75 | Medium | 60-80 hrs |
| 76+ | Use full questionnaire | |

---

## One-Page Sign-Off

```
PROJECT: _______________________
DATE: _________________________

SCOPE SUMMARY:
• Users/Roles: _______________________________________________
• Main Features: _____________________________________________
• Screens: ___________________________________________________
• NOT included: ______________________________________________

ESTIMATE: _______ hours
BUDGET: $_______
DEADLINE: _______

By signing, client confirms:
1. Requirements above are complete
2. Items not listed are out of scope
3. Changes require written approval

Client: _____________________ Date: _______
Developer: __________________ Date: _______
```

---

## Red Flags (Stop & Use Full Questionnaire)

- More than 3 user roles
- More than 5 entities
- Complex workflows (more than 3 states)
- Multiple integrations
- Compliance requirements (HIPAA, GDPR, etc.)
- Native mobile app needed
- Multi-tenant/multi-organization
- Real-time features
- Client can't answer basic questions

---

*Use full Requirements_Questionnaire.md for anything complex*
