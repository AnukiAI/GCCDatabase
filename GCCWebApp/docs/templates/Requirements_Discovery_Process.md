# Requirements Discovery Process Guide

**Purpose:** Step-by-step facilitation guide for conducting requirements discovery sessions using the Requirements Questionnaire. This guide ensures consistent, thorough requirements capture.

---

## Table of Contents

1. [Discovery Process Overview](#1-discovery-process-overview)
2. [Pre-Discovery Phase](#2-pre-discovery-phase)
3. [Discovery Session Structure](#3-discovery-session-structure)
4. [Facilitation Techniques](#4-facilitation-techniques)
5. [Entity & Data Modeling Workshop](#5-entity--data-modeling-workshop)
6. [View Design Workshop](#6-view-design-workshop)
7. [Workflow Mapping Workshop](#7-workflow-mapping-workshop)
8. [Post-Discovery Phase](#8-post-discovery-phase)
9. [Templates & Worksheets](#9-templates--worksheets)
10. [Common Pitfalls & Solutions](#10-common-pitfalls--solutions)

---

## 1. Discovery Process Overview

### 1.1 Total Time Investment

| Phase | Duration | Sessions | Participants |
|-------|----------|----------|--------------|
| Pre-Discovery | 2-4 hours | 1 internal | Developer only |
| Discovery Sessions | 6-12 hours | 3-5 sessions | Developer + Client |
| Post-Discovery | 4-8 hours | 1-2 internal | Developer only |
| **TOTAL** | **12-24 hours** | **5-8 sessions** | |

**Note:** This investment prevents 50-100+ hours of rework. GCC project proof: 6 months could have been 3-4 months with proper discovery.

### 1.2 Session Breakdown

```
┌─────────────────────────────────────────────────────────────────┐
│                    DISCOVERY PROCESS FLOW                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  PRE-DISCOVERY (Developer Only)                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 1. Review any existing documentation                      │   │
│  │ 2. Prepare materials and worksheets                       │   │
│  │ 3. Send pre-session questionnaire to client               │   │
│  │ 4. Schedule sessions                                      │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ▼                                   │
│  SESSION 1: FOUNDATION (2-3 hours)                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Project Overview (Sections 1)                           │   │
│  │ • Success Metrics & KPIs                                  │   │
│  │ • Stakeholder Mapping                                     │   │
│  │ • Constraints & Risks                                     │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ▼                                   │
│  SESSION 2: DATA & BUSINESS RULES (2-3 hours)                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Entity Identification Workshop                          │   │
│  │ • Relationship Mapping                                    │   │
│  │ • Business Rules & Validation                             │   │
│  │ • Workflow State Diagrams                                 │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ▼                                   │
│  SESSION 3: USERS & ACCESS (1.5-2 hours)                        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • User Role Definition                                    │   │
│  │ • Permission Matrix                                       │   │
│  │ • Authentication Requirements                             │   │
│  │ • Data Visibility Rules                                   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ▼                                   │
│  SESSION 4: VIEWS & FORMS (2-3 hours)                           │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Page/View Inventory                                     │   │
│  │ • Per-View Deep Dive (Section 5.2 for each)              │   │
│  │ • Form Design Workshop                                    │   │
│  │ • Navigation Flow Mapping                                 │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ▼                                   │
│  SESSION 5: FEATURES & TECHNICAL (1.5-2 hours)                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Notifications & Communications                          │   │
│  │ • Import/Export Requirements                              │   │
│  │ • Integrations                                            │   │
│  │ • Mobile/Responsive                                       │   │
│  │ • Security & Compliance                                   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ▼                                   │
│  POST-DISCOVERY (Developer Only)                                │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 1. Compile questionnaire                                  │   │
│  │ 2. Calculate complexity score                             │   │
│  │ 3. Create estimate                                        │   │
│  │ 4. Prepare sign-off document                              │   │
│  │ 5. Review session with client                             │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ▼                                   │
│  SIGN-OFF SESSION (1 hour)                                      │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Walk through completed questionnaire                    │   │
│  │ • Review complexity score                                 │   │
│  │ • Discuss estimate                                        │   │
│  │ • Obtain sign-off                                         │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Pre-Discovery Phase

### 2.1 Pre-Discovery Checklist

```
□ Review any existing documentation client has provided
□ Research client's industry/domain
□ Research competitors (if applicable)
□ Prepare discovery materials:
  □ Print/share Requirements Questionnaire
  □ Prepare whiteboard/digital canvas for workshops
  □ Prepare Entity Relationship template
  □ Prepare State Diagram template
  □ Prepare Permission Matrix template
□ Send Pre-Session Questionnaire to client (see 2.2)
□ Schedule all discovery sessions
□ Send calendar invites with session agendas
□ Confirm decision-maker will attend
```

### 2.2 Pre-Session Questionnaire (Send to Client 3-5 Days Before)

**Email Template:**

```
Subject: Preparation for Requirements Discovery - [Project Name]

Hi [Client Name],

Looking forward to our discovery sessions. To make the most of our time together,
please complete the following before our first session:

1. BRING TO SESSION 1:
   - Any existing documentation, mockups, or screenshots
   - List of all people who will use this system
   - 3 websites/apps you like the look/feel of
   - Your top 3 must-have features
   - Your biggest concern about this project

2. PLEASE ANSWER THESE QUESTIONS (reply to this email):

   a) In one sentence, what problem does this solve?

   b) Who requested this project and why now?

   c) What happens if this project is NOT completed?

   d) Are there any hard deadlines? What drives them?

   e) What's the budget range? (Rough order of magnitude is fine)

   f) Who is the final decision-maker for this project?

   g) Is there an existing system this replaces? If so, what do you
      want to KEEP vs CHANGE?

3. OPTIONAL BUT HELPFUL:
   - Org chart showing who will use the system
   - Sample data files (spreadsheets, reports)
   - Screenshots of current system (if replacing something)

See you on [Date] at [Time].

Best,
[Your Name]
```

### 2.3 Materials Preparation

**Physical Session:**
- Whiteboard + markers (multiple colors)
- Large sticky notes (3 colors minimum)
- Printed worksheets (see Section 9)
- Laptop for note-taking
- Camera/phone for capturing whiteboard drawings

**Remote Session:**
- Digital whiteboard (Miro, FigJam, Excalidraw)
- Screen sharing setup
- Recording permission (ask client)
- Shared document for live note-taking

---

## 3. Discovery Session Structure

### 3.1 Session 1: Foundation (2-3 hours)

**Agenda:**

```
00:00 - 00:15  Introductions & Session Overview
               - Who's in the room and their role
               - Overview of discovery process
               - Set expectations: "No question is too basic"

00:15 - 00:45  Project Vision & Problem Statement
               - "What problem are we solving?"
               - "What does success look like?"
               - "What happens if we don't do this?"
               - Capture in client's words (exact quotes)

00:45 - 01:15  Stakeholder Mapping Exercise
               - Who uses this system?
               - Who is affected by it?
               - Who makes decisions?
               - Who can block progress?
               - Map on stakeholder matrix (see 9.1)

01:15 - 01:30  BREAK

01:30 - 02:00  Constraints Discussion
               - Hard deadlines and what drives them
               - Budget constraints
               - Technology constraints
               - Regulatory/compliance requirements
               - Team/resource constraints

02:00 - 02:30  Success Metrics & KPIs
               - How will we measure success?
               - What numbers matter?
               - Current baseline vs. targets
               - When do you expect to hit targets?

02:30 - 02:45  Reference Review
               - Review the 3 websites/apps they brought
               - For each: "What specifically do you like?"
               - Capture: Layout? Colors? Features? Feel?

02:45 - 03:00  Session Wrap-up
               - Summarize key points
               - Preview next session
               - Assign homework (if any)
               - Confirm next session date/time
```

**Key Questions to Ask:**

| Topic | Question | Why It Matters |
|-------|----------|----------------|
| Problem | "What happens today without this system?" | Grounds the problem in reality |
| Problem | "Walk me through a typical day dealing with this problem" | Reveals hidden pain points |
| Success | "If this project is wildly successful, what's different in 6 months?" | Defines success criteria |
| Success | "How would you know if we failed?" | Identifies risks |
| Scope | "What's the ONE thing this must do?" | Forces prioritization |
| Scope | "What could we cut and still call it a success?" | Identifies MVP |
| Users | "Who complains most about the current process?" | Key stakeholder to satisfy |
| Timeline | "What happens on [deadline date] that makes it hard?" | Validates deadline |

### 3.2 Session 2: Data & Business Rules (2-3 hours)

**Agenda:**

```
00:00 - 00:15  Recap Session 1 & Preview Session 2
               - Key points from last session
               - Today's goal: Understand your data

00:15 - 01:00  Entity Identification Workshop
               - "What are the main THINGS in your system?"
               - Use the Noun Exercise (see 4.2)
               - List all entities on whiteboard
               - For each entity: "What information do you track?"
               - Group related entities

01:00 - 01:30  Relationship Mapping Exercise
               - For each pair of entities: "How do these relate?"
               - Draw Entity Relationship Diagram (see 5.1)
               - Identify: 1:1, 1:N, N:N relationships
               - Ask: "Can one [A] have many [B]s? Can one [B] have many [A]s?"

01:30 - 01:45  BREAK

01:45 - 02:15  Business Rules Deep Dive
               - For each entity: "What rules apply?"
               - Validation rules
               - Calculated fields
               - Constraints (limits, restrictions)
               - Use the "What happens if..." technique (see 4.3)

02:15 - 02:45  Workflow & State Mapping
               - "Does [entity] have a status or state?"
               - Draw State Diagram (see 7.1)
               - For each transition: "Who can do this? When?"
               - Identify approval workflows

02:45 - 03:00  Session Wrap-up
               - Review diagrams created
               - Identify any entities/rules missed
               - Preview next session
```

**Entity Identification Script:**

```
"Let's identify all the THINGS your system needs to track.
I'll ask you questions, and you tell me the nouns.

1. When a user comes to your system, what are they looking for?
   → [Write down every noun they mention]

2. What do you currently track in spreadsheets or documents?
   → [More nouns]

3. What reports do you need to generate? What data is on them?
   → [More nouns]

4. Who interacts with your system? What do they create/manage?
   → [More nouns]

Now let's group these. [Group related nouns]

For each group, let's define the core entity and its attributes..."
```

### 3.3 Session 3: Users & Access (1.5-2 hours)

**Agenda:**

```
00:00 - 00:15  Recap & Preview
               - Review entities and workflows from Session 2
               - Today's goal: Define who does what

00:15 - 00:45  User Role Definition Exercise
               - List all user types
               - For each role: "What can they do? What can't they do?"
               - Build Permission Matrix (see 9.2)

00:45 - 01:15  Data Visibility Workshop
               - "What data can [Role] see?"
               - "Can they see other users' data?"
               - "Can they see other departments' data?"
               - Map visibility rules per role

01:15 - 01:30  Authentication Requirements
               - Login method (username, email, SSO)
               - Password policies
               - 2FA requirements
               - Session management

01:30 - 01:45  Organization & Multi-tenancy
               - "Do users belong to companies/organizations?"
               - "Can users be in multiple organizations?"
               - "Is data shared across organizations?"

01:45 - 02:00  Session Wrap-up
               - Review Permission Matrix
               - Confirm visibility rules
               - Preview next session
```

**Permission Matrix Script:**

```
"Let's build a permission matrix. I'll list actions on one axis
and roles on the other. For each cell, tell me: Yes, No, or Conditional.

         | Create | Read | Update | Delete | Approve |
---------|--------|------|--------|--------|---------|
Admin    |   ?    |  ?   |   ?    |   ?    |    ?    |
Manager  |   ?    |  ?   |   ?    |   ?    |    ?    |
User     |   ?    |  ?   |   ?    |   ?    |    ?    |
Viewer   |   ?    |  ?   |   ?    |   ?    |    ?    |

For any 'Conditional' answer: What's the condition?
- Only their own data?
- Only their department?
- Only certain statuses?
- Only with approval?
"
```

### 3.4 Session 4: Views & Forms (2-3 hours)

**Agenda:**

```
00:00 - 00:15  Recap & Preview
               - Review users, roles, permissions
               - Today's goal: Define every screen

00:15 - 00:45  Page Inventory Exercise
               - "Walk me through using the system from login to logout"
               - Capture every screen mentioned
               - Organize into: Public, Auth Required, Role-Specific
               - Identify landing page per role

00:45 - 01:45  View Deep Dive (repeat for each major view)
               - Use View Design Worksheet (see 9.3)
               - Cover: Display, Pagination, Sorting, Filtering, Search
               - Cover: Selection, Bulk Actions, Row Actions
               - Cover: Responsive requirements

01:45 - 02:00  BREAK

02:00 - 02:30  Form Design Workshop
               - For each form: List all fields
               - Required vs optional
               - Validation rules
               - Conditional logic
               - Multi-step wizards

02:30 - 02:45  Navigation Flow Mapping
               - How do users move between screens?
               - Draw navigation diagram
               - Identify entry points, exit points

02:45 - 03:00  Session Wrap-up
               - Review all views identified
               - Confirm nothing missing
               - Preview next session
```

**View Deep Dive Script:**

```
"Let's go deep on [View Name]. I'm going to ask you about
every aspect of this screen.

DISPLAY:
1. What data appears on this page? (List every field)
2. How is it displayed? Table? Cards? List?
3. How many records typically? Maximum?

PAGINATION:
4. Do you want pages (1, 2, 3) or infinite scroll?
5. How many items per page?

SORTING:
6. Which columns should be sortable?
7. What's the default sort?

FILTERING:
8. What can users filter by? (List every filter)
9. For each filter: dropdown, text, date range, checkbox?
10. Where do filter options come from? (hardcoded or database?)
11. Can users save their favorite filters?
12. Should the filter state be in the URL so it's shareable?

SEARCH:
13. What should the search box search?
14. Search as you type, or press enter?
15. Show suggestions while typing?

SELECTION:
16. Can users select rows? One or multiple?
17. What can they do with selected rows? (bulk actions)

ACTIONS:
18. What can users do to individual rows? (view, edit, delete, etc.)
19. Can they edit inline or does it go to a new page?

RESPONSIVE:
20. What happens on mobile? Same layout or different?
"
```

### 3.5 Session 5: Features & Technical (1.5-2 hours)

**Agenda:**

```
00:00 - 00:15  Recap & Preview
               - Review views and forms
               - Today's goal: Everything else

00:15 - 00:35  Notifications & Communication
               - What emails does the system send?
               - In-app notifications?
               - SMS required?
               - User notification preferences?

00:35 - 00:55  Import/Export Requirements
               - What data needs to be imported? Format?
               - What data needs to be exported? Format?
               - Bulk operations?

00:55 - 01:15  Integrations
               - What third-party systems connect?
               - Do you need a public API?
               - Any scheduled syncs?

01:15 - 01:35  Mobile & Responsive
               - Who uses this on mobile?
               - Same features or reduced?
               - Native app or web?
               - Offline requirements?

01:35 - 01:50  Security & Compliance
               - Any regulatory requirements?
               - Audit logging needs?
               - Data retention requirements?

01:50 - 02:00  Session Wrap-up
               - Final questions
               - Explain next steps (post-discovery)
               - Schedule sign-off session
```

---

## 4. Facilitation Techniques

### 4.1 The "5 Whys" Technique

When a client gives a vague answer, ask "Why?" up to 5 times.

**Example:**

```
Client: "We need a dashboard."
You: "Why do you need a dashboard?"
Client: "To see how we're doing."
You: "Why do you need to see how you're doing?"
Client: "To know if we're on track."
You: "Why do you need to know if you're on track?"
Client: "Because we have monthly targets."
You: "Why are monthly targets important?"
Client: "Because we get penalized if we miss them."

→ NOW you understand: They need a dashboard showing progress
   toward monthly targets with alerts when they're at risk.
```

### 4.2 The Noun Exercise

For entity identification:

```
Step 1: Have client describe their process in normal sentences
Step 2: Circle every NOUN they say
Step 3: Group related nouns
Step 4: For each group, ask: "Is this something you need to track?"
Step 5: Surviving nouns become entities
```

**Example:**

```
Client: "A CUSTOMER submits an ORDER for PRODUCTS. We assign it
to a WAREHOUSE for FULFILLMENT. The SHIPPING COMPANY picks it up
and delivers to the CUSTOMER's ADDRESS."

Nouns: Customer, Order, Products, Warehouse, Fulfillment,
       Shipping Company, Address

Entities: Customer, Order, Product, Warehouse, ShippingCompany
(Address is likely an attribute of Customer, not its own entity)
(Fulfillment is likely a status of Order, not its own entity)
```

### 4.3 The "What Happens If..." Technique

For uncovering business rules and edge cases:

```
"What happens if..."
- ...the user enters invalid data?
- ...two users edit the same record?
- ...the approval is rejected?
- ...the deadline passes?
- ...the payment fails?
- ...the file is too large?
- ...the user's session expires mid-form?
- ...the external system is down?
- ...data needs to be deleted?
- ...a user leaves the company?
```

### 4.4 The "Show Me" Technique

When clients use vague terms:

```
DON'T: Accept "We need a good filter"
DO: "Can you show me an example of a filter you like?
    Let's look at Amazon/Google/[competitor] together."

DON'T: Accept "Make it mobile-friendly"
DO: "Can you show me what you do on your phone right now
    that you'd want to do in this system?"

DON'T: Accept "Standard reporting"
DO: "Can you show me an actual report you currently create?
    Walk me through how you make it."
```

### 4.5 The "Concrete Example" Technique

Replace abstract discussions with specific examples:

```
DON'T: "How should the approval workflow work?"
DO: "Let's take a real example. [Name] just submitted a certificate
    for approval. Walk me through exactly what happens next,
    step by step, until it's approved or rejected."

DON'T: "What fields should be on the form?"
DO: "I'm going to pretend to be a user. Tell me exactly what
    information I need to enter to [complete action]."

DON'T: "What data do you need on the report?"
DO: "You have a meeting with [stakeholder] tomorrow. They ask
    for a report on [topic]. What columns do you need to see?"
```

### 4.6 The "Silence" Technique

After asking a question, wait. Count to 10 silently if needed.

- Clients need time to think
- Silence creates pressure to elaborate
- First answer is often surface-level
- Real requirements come after uncomfortable pauses

### 4.7 The "Summarize Back" Technique

Repeat what you heard and ask for confirmation:

```
"Let me make sure I understand. You're saying that:
1. Only managers can approve orders over $1000
2. Orders under $1000 auto-approve
3. If rejected, the order goes back to the submitter
4. Submitter can edit and resubmit unlimited times

Did I get that right? Anything I missed?"
```

### 4.8 Handling "I Don't Know" Answers

```
Client: "I don't know"

Response Options:
1. "That's okay. Who would know?" → Schedule follow-up with that person
2. "What would you prefer?" → Get their opinion, even if not final
3. "Let me give you options: A, B, or C. Which sounds closest?"
4. "Let's skip this for now and come back." → Flag for follow-up
5. "What do similar systems do?" → Research together
```

### 4.9 Handling Disagreement Between Stakeholders

```
When two stakeholders disagree:

1. Acknowledge both perspectives
   "I hear that [Person A] wants X and [Person B] wants Y."

2. Understand the WHY behind each
   "Person A, why is X important to you?"
   "Person B, why is Y important to you?"

3. Find common ground
   "It sounds like you both want [underlying goal].
   Let me suggest a way to achieve that..."

4. If no resolution, escalate to decision-maker
   "This is an important decision. Can we get [Decision Maker]'s
   input on this before our next session?"

5. Document the disagreement
   "I'm noting this as an open question. We'll need to resolve
   it before we can finalize the requirements."
```

---

## 5. Entity & Data Modeling Workshop

### 5.1 Entity Relationship Diagram Template

```
┌─────────────────────────────────────────────────────────────────┐
│                 ENTITY RELATIONSHIP DIAGRAM                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌──────────────┐         ┌──────────────┐                     │
│   │   ENTITY A   │         │   ENTITY B   │                     │
│   ├──────────────┤         ├──────────────┤                     │
│   │ • field 1    │         │ • field 1    │                     │
│   │ • field 2    │         │ • field 2    │                     │
│   │ • field 3    │         │ • field 3    │                     │
│   │ • field 4    │         │ • field 4    │                     │
│   └──────┬───────┘         └───────┬──────┘                     │
│          │                         │                             │
│          │    RELATIONSHIP         │                             │
│          │  ┌──────────────┐       │                             │
│          └──┤   1 : N      ├───────┘                             │
│             │  (one to many)│                                    │
│             └──────────────┘                                     │
│                                                                  │
│   RELATIONSHIP TYPES:                                            │
│   ──────── 1:1 (one to one)                                     │
│   ──────<  1:N (one to many)                                    │
│   >─────<  N:N (many to many)                                   │
│   ──○──── Optional relationship                                  │
│   ──●──── Required relationship                                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Entity Definition Worksheet

Complete for EACH entity:

```
┌─────────────────────────────────────────────────────────────────┐
│ ENTITY NAME: _______________________________________________    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ DESCRIPTION (in plain English):                                  │
│ ________________________________________________________________ │
│ ________________________________________________________________ │
│                                                                  │
│ FIELDS/ATTRIBUTES:                                               │
│ ┌────────────────┬──────────┬──────────┬──────────┬───────────┐ │
│ │ Field Name     │ Type     │ Required │ Unique   │ Notes     │ │
│ ├────────────────┼──────────┼──────────┼──────────┼───────────┤ │
│ │                │          │ □ Yes    │ □ Yes    │           │ │
│ │                │          │ □ Yes    │ □ Yes    │           │ │
│ │                │          │ □ Yes    │ □ Yes    │           │ │
│ │                │          │ □ Yes    │ □ Yes    │           │ │
│ │                │          │ □ Yes    │ □ Yes    │           │ │
│ │                │          │ □ Yes    │ □ Yes    │           │ │
│ └────────────────┴──────────┴──────────┴──────────┴───────────┘ │
│                                                                  │
│ CALCULATED FIELDS (auto-computed):                               │
│ • ______________________________ = Formula: _________________   │
│ • ______________________________ = Formula: _________________   │
│                                                                  │
│ RELATIONSHIPS:                                                   │
│ • This entity has MANY _________________________ (1:N)          │
│ • This entity belongs to ONE _____________________ (N:1)        │
│ • This entity has many-to-many with _____________ (N:N)         │
│                                                                  │
│ BUSINESS RULES:                                                  │
│ • ________________________________________________________________ │
│ • ________________________________________________________________ │
│ • ________________________________________________________________ │
│                                                                  │
│ EXPECTED VOLUME:                                                 │
│ • Current record count: ____________                             │
│ • Growth rate: ____________ per month                           │
│ • Max expected: ____________                                     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.3 Relationship Questionnaire

For EACH pair of potentially related entities:

```
Entity A: ________________    Entity B: ________________

1. Can one [A] have multiple [B]s?              □ Yes  □ No
2. Can one [B] have multiple [A]s?              □ Yes  □ No
3. Is this relationship required or optional?    □ Required  □ Optional
4. When [A] is deleted, what happens to [B]?    □ Delete  □ Keep  □ Block
5. Is there data specific to this relationship? □ Yes  □ No
   If yes, what? _______________________________________

RESULT:
□ 1:1 (Both questions = No)
□ 1:N (Question 1 = No, Question 2 = Yes)  → A has many Bs
□ N:1 (Question 1 = Yes, Question 2 = No)  → B has many As
□ N:N (Both questions = Yes)               → Junction table needed
```

---

## 6. View Design Workshop

### 6.1 View Inventory Exercise

```
Step 1: Start with roles
        "As an [Admin/User/Viewer], what pages do you need?"

Step 2: List every page mentioned
        Write on sticky notes, one page per note

Step 3: Organize by category
        - Public (no login)
        - Authenticated (any logged-in user)
        - Role-specific (Admin only, etc.)

Step 4: Identify entry points
        - What's the first page each role sees?
        - What's the most-used page?

Step 5: Number/prioritize
        - Which pages are must-have for MVP?
        - Which can wait for Phase 2?
```

### 6.2 View Design Worksheet

Complete for EACH view:

```
┌─────────────────────────────────────────────────────────────────┐
│ VIEW NAME: _______________________________________________      │
│ URL: /_________________________________________________         │
│ ACCESS: □ Public  □ Auth Required  □ Role: ______________       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ PURPOSE (one sentence):                                          │
│ ________________________________________________________________ │
│                                                                  │
│ DATA DISPLAYED:                                                  │
│ Entity: ________________  Fields shown: _______________________ │
│ ______________________________________________________________ │
│                                                                  │
│ DISPLAY FORMAT:  □ Table  □ Cards  □ List  □ Grid  □ Other     │
│                                                                  │
│ RECORD COUNT:  Typical: _______  Maximum: _______               │
│                                                                  │
├───────────────────────────────────────────────────────────────── │
│ PAGINATION                                                       │
│ Style: □ Traditional pages  □ Infinite scroll  □ Load more      │
│ Items per page: _______  User can change? □ Yes  □ No           │
├───────────────────────────────────────────────────────────────── │
│ SORTING                                                          │
│ Sortable columns: _____________________________________________ │
│ Default sort: _________________ □ ASC  □ DESC                   │
│ Multi-column sort? □ Yes  □ No                                  │
├───────────────────────────────────────────────────────────────── │
│ FILTERING                                                        │
│ ┌─────────────────┬───────────────┬───────────────┬───────────┐ │
│ │ Filter Field    │ Filter Type   │ Source        │ Multi?    │ │
│ ├─────────────────┼───────────────┼───────────────┼───────────┤ │
│ │                 │ □ Drop □ Text │ □ Hard □ DB   │ □ Y □ N   │ │
│ │                 │ □ Date □ Check│ □ Hard □ DB   │ □ Y □ N   │ │
│ │                 │ □ Drop □ Text │ □ Hard □ DB   │ □ Y □ N   │ │
│ │                 │ □ Date □ Check│ □ Hard □ DB   │ □ Y □ N   │ │
│ └─────────────────┴───────────────┴───────────────┴───────────┘ │
│ Filter logic: □ AND only  □ OR only  □ User configurable        │
│ Save filters? □ Yes  □ No    Filter state in URL? □ Yes  □ No   │
├───────────────────────────────────────────────────────────────── │
│ SEARCH                                                           │
│ Search enabled? □ Yes  □ No                                     │
│ Search fields: ________________________________________________ │
│ Search type: □ Contains  □ Starts with  □ Exact  □ Fuzzy        │
│ Autocomplete/suggestions? □ Yes  □ No                           │
│ Search state in URL? □ Yes  □ No                                │
├───────────────────────────────────────────────────────────────── │
│ SELECTION & BULK ACTIONS                                        │
│ Row selection: □ None  □ Single  □ Multiple                     │
│ Select all: □ Current page only  □ All pages                    │
│ Bulk actions: _________________________________________________ │
├───────────────────────────────────────────────────────────────── │
│ ROW ACTIONS                                                      │
│ □ View  □ Edit  □ Delete  □ Duplicate  □ Other: ____________   │
│ Inline editing? □ Yes  □ No    Row expansion? □ Yes  □ No       │
├───────────────────────────────────────────────────────────────── │
│ RESPONSIVE                                                       │
│ Mobile layout: □ Same  □ Different  □ Hide on mobile            │
│ Touch gestures: □ Yes  □ No                                     │
│ Minimum width: _________                                         │
├───────────────────────────────────────────────────────────────── │
│ URL REQUIREMENTS                                                 │
│ □ Must be shareable/bookmarkable                                │
│ □ Must support deep linking to specific records                 │
│ □ Must preserve filter/search state                             │
├───────────────────────────────────────────────────────────────── │
│ SKETCH (rough layout):                                           │
│                                                                  │
│                                                                  │
│                                                                  │
│                                                                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Workflow Mapping Workshop

### 7.1 State Diagram Template

```
┌─────────────────────────────────────────────────────────────────┐
│             STATE DIAGRAM FOR: _______________                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│    ┌─────────┐                                                  │
│    │  START  │                                                  │
│    └────┬────┘                                                  │
│         │ [action: create]                                      │
│         │ [who: user]                                           │
│         ▼                                                       │
│    ┌─────────┐         ┌─────────┐         ┌─────────┐         │
│    │ STATE 1 │────────→│ STATE 2 │────────→│ STATE 3 │         │
│    └─────────┘         └────┬────┘         └─────────┘         │
│         │                   │                                    │
│         │ [action]          │ [reject]                          │
│         │ [who]             │ [who]                             │
│         ▼                   ▼                                    │
│    ┌─────────┐         ┌─────────┐                              │
│    │ STATE 4 │         │ STATE 5 │                              │
│    └────┬────┘         └─────────┘                              │
│         │                                                        │
│         │                                                        │
│         ▼                                                        │
│    ┌─────────┐                                                  │
│    │   END   │                                                  │
│    └─────────┘                                                  │
│                                                                  │
│  LEGEND:                                                        │
│  ┌─────┐ = State/Status                                        │
│  ──────→ = Transition                                           │
│  [action] = What triggers the transition                        │
│  [who] = Who can perform this action                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 7.2 State Definition Worksheet

For each state in the diagram:

```
┌─────────────────────────────────────────────────────────────────┐
│ STATE NAME: _______________________________________________     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ DESCRIPTION:                                                     │
│ ________________________________________________________________ │
│                                                                  │
│ ENTRY CONDITIONS (how do records get here?):                    │
│ • From state: _______________ via action: ___________________   │
│ • From state: _______________ via action: ___________________   │
│                                                                  │
│ EXIT TRANSITIONS (where can records go?):                       │
│ ┌───────────────┬─────────────────┬─────────────┬─────────────┐ │
│ │ To State      │ Action/Trigger  │ Who Can Do  │ Conditions  │ │
│ ├───────────────┼─────────────────┼─────────────┼─────────────┤ │
│ │               │                 │             │             │ │
│ │               │                 │             │             │ │
│ │               │                 │             │             │ │
│ └───────────────┴─────────────────┴─────────────┴─────────────┘ │
│                                                                  │
│ ACTIONS ON ENTRY (what happens automatically?):                 │
│ □ Send email to: _______________________________________________│
│ □ Create notification for: _____________________________________│
│ □ Update field: _______________ to: ___________________________│
│ □ Start timer for: ____________________________________________│
│ □ Other: ______________________________________________________│
│                                                                  │
│ RESTRICTIONS IN THIS STATE:                                     │
│ □ Record is read-only                                           │
│ □ Only these fields editable: __________________________________│
│ □ Only these roles can view: ___________________________________│
│ □ Other: ______________________________________________________│
│                                                                  │
│ TIME LIMITS/SLA:                                                │
│ □ Must exit within: ____________________________________________│
│ □ If exceeded, notify: _________________________________________│
│ □ If exceeded, auto-transition to: _____________________________│
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 7.3 Workflow Questionnaire Script

```
"Let's map the lifecycle of [Entity]. I'll ask questions to
understand every status it can have.

1. When a [Entity] is first created, what status is it in?
   → [First state]

2. What happens next? What status does it move to?
   → [Second state]

3. Who makes it move from [First] to [Second]?
   → [Role]

4. What action do they take?
   → [Action name]

5. After [Second], what are the possible next statuses?
   → [Branch states]

6. For each branch: What causes it to go this direction?
   → [Conditions]

7. When is it 'done'? What's the final status?
   → [End states]

8. Can it ever go backwards? From which states to which?
   → [Backward transitions]

9. Are there any statuses where it can get 'stuck'?
   → [Dead ends]

10. Do any transitions happen automatically?
    → [Auto-transitions]

11. Do any transitions require approval?
    → [Approval workflows]

12. What emails/notifications happen at each step?
    → [Notifications per state]
"
```

---

## 8. Post-Discovery Phase

### 8.1 Post-Discovery Checklist

```
□ Compile all session notes
□ Complete Requirements Questionnaire from notes
□ Fill in any gaps (mark assumptions clearly)
□ Draw clean diagrams:
  □ Entity Relationship Diagram
  □ State Diagrams (per entity)
  □ Navigation Flow Diagram
  □ Permission Matrix
□ Calculate complexity score
□ Create estimate based on score
□ Identify risks and assumptions
□ Prepare sign-off document
□ Schedule sign-off session
□ Send draft to client for review before sign-off session
```

### 8.2 Complexity Score Calculation

```
1. Go through completed questionnaire
2. Add up all complexity points
3. Use this guide:

   0-75 points:    SIMPLE         ~40-80 hours
   76-150 points:  MODERATE       ~80-200 hours
   151-300 points: COMPLEX        ~200-500 hours
   301-500 points: VERY COMPLEX   ~500-1000 hours
   500+ points:    PROGRAM        ~1000+ hours (multiple phases)

4. Adjust for:
   - Team experience with tech stack (+/- 20%)
   - Client responsiveness (+/- 15%)
   - Requirements clarity (+/- 25%)
   - Technical risk (+/- 20%)

5. Add contingency:
   - Simple projects: +20%
   - Complex projects: +30-40%
```

### 8.3 Sign-Off Document Template

```
┌─────────────────────────────────────────────────────────────────┐
│           REQUIREMENTS SIGN-OFF DOCUMENT                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Project: ____________________________________________________   │
│ Date: _______________________________________________________   │
│ Prepared by: ________________________________________________   │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│ EXECUTIVE SUMMARY                                                │
│                                                                  │
│ Total Pages/Views: ______                                        │
│ Total Forms: ______                                              │
│ Total Entities: ______                                           │
│ User Roles: ______                                               │
│ Integrations: ______                                             │
│ Complexity Score: ______ (Category: _____________)              │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│ KEY REQUIREMENTS SUMMARY                                         │
│                                                                  │
│ Must-Have (MVP):                                                 │
│ • ___________________________________________________________   │
│ • ___________________________________________________________   │
│ • ___________________________________________________________   │
│                                                                  │
│ Should-Have (Phase 2):                                          │
│ • ___________________________________________________________   │
│ • ___________________________________________________________   │
│                                                                  │
│ Nice-to-Have (Future):                                          │
│ • ___________________________________________________________   │
│ • ___________________________________________________________   │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│ ASSUMPTIONS                                                      │
│                                                                  │
│ The following assumptions were made. If incorrect, scope may    │
│ change:                                                          │
│ • ___________________________________________________________   │
│ • ___________________________________________________________   │
│ • ___________________________________________________________   │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│ OPEN QUESTIONS                                                   │
│                                                                  │
│ The following questions remain unanswered:                      │
│ • ___________________________________________________________   │
│ • ___________________________________________________________   │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│ RISKS                                                            │
│                                                                  │
│ • ___________________________________________________________   │
│ • ___________________________________________________________   │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│ SIGN-OFF                                                         │
│                                                                  │
│ By signing below, the client acknowledges:                      │
│ 1. All requirements in the attached questionnaire are accurate  │
│ 2. Features NOT listed are OUT OF SCOPE                         │
│ 3. Changes after sign-off require a Change Request              │
│ 4. Change Requests may affect timeline and budget               │
│                                                                  │
│ Client Signature: _____________________ Date: ___________       │
│ Client Name (Print): ________________________________________   │
│ Client Title: _______________________________________________   │
│                                                                  │
│ Developer Signature: ____________________ Date: ___________     │
│ Developer Name: _____________________________________________   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 8.4 Change Request Process

After sign-off, use this process for any changes:

```
┌─────────────────────────────────────────────────────────────────┐
│                 CHANGE REQUEST PROCESS                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Client Request                                                  │
│       │                                                          │
│       ▼                                                          │
│  ┌─────────────────────────────────┐                            │
│  │ Developer Documents Change      │                            │
│  │ • What is being changed?        │                            │
│  │ • Why is it being changed?      │                            │
│  │ • Impact on scope               │                            │
│  │ • Impact on timeline            │                            │
│  │ • Impact on budget              │                            │
│  └───────────────┬─────────────────┘                            │
│                  │                                               │
│                  ▼                                               │
│  ┌─────────────────────────────────┐                            │
│  │ Client Reviews Impact           │                            │
│  └───────────────┬─────────────────┘                            │
│                  │                                               │
│         ┌───────┴───────┐                                       │
│         │               │                                        │
│         ▼               ▼                                        │
│    ┌─────────┐    ┌─────────┐                                   │
│    │ Approve │    │ Reject  │                                   │
│    └────┬────┘    └────┬────┘                                   │
│         │              │                                         │
│         ▼              ▼                                         │
│    ┌─────────────┐  ┌─────────────┐                             │
│    │ Sign Change │  │ No Change   │                             │
│    │ Request     │  │ Made        │                             │
│    └─────────────┘  └─────────────┘                             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Change Request Template:**

```
┌─────────────────────────────────────────────────────────────────┐
│                    CHANGE REQUEST                                │
├─────────────────────────────────────────────────────────────────┤
│ CR Number: CR-______                                             │
│ Date: ____________                                               │
│ Requested By: ____________                                       │
│ Priority: □ Low  □ Medium  □ High  □ Critical                   │
├─────────────────────────────────────────────────────────────────┤
│ DESCRIPTION OF CHANGE:                                           │
│ ________________________________________________________________ │
│ ________________________________________________________________ │
│                                                                  │
│ REASON FOR CHANGE:                                               │
│ ________________________________________________________________ │
│ ________________________________________________________________ │
├─────────────────────────────────────────────────────────────────┤
│ IMPACT ANALYSIS:                                                 │
│                                                                  │
│ Affected Components:                                             │
│ • _______________________________________________________________│
│ • _______________________________________________________________│
│                                                                  │
│ Effort Impact: ______ hours                                      │
│ Timeline Impact: ______ days                                     │
│ Budget Impact: $______                                           │
│ Risk Impact: □ Low  □ Medium  □ High                            │
├─────────────────────────────────────────────────────────────────┤
│ DECISION:                                                        │
│ □ Approved    □ Rejected    □ Deferred                          │
│                                                                  │
│ Client Signature: _____________________ Date: ___________       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 9. Templates & Worksheets

### 9.1 Stakeholder Matrix

```
┌─────────────────────────────────────────────────────────────────┐
│                    STAKEHOLDER MATRIX                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│                    HIGH POWER                                    │
│                        │                                         │
│    KEEP SATISFIED     │    MANAGE CLOSELY                       │
│    ┌─────────────────┬┼──────────────────┐                      │
│    │                 ││                  │                      │
│    │                 ││                  │                      │
│    │                 ││                  │                      │
│ LOW├─────────────────┼┼──────────────────┤HIGH                  │
│ INT│                 ││                  │INT                   │
│ ERE│                 ││                  │ERE                   │
│ ST │                 ││                  │ST                    │
│    │                 ││                  │                      │
│    │                 ││                  │                      │
│    └─────────────────┴┼──────────────────┘                      │
│    MONITOR            │    KEEP INFORMED                        │
│                       │                                          │
│                    LOW POWER                                     │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│ STAKEHOLDER LIST:                                                │
│                                                                  │
│ Name: _______________ Role: _______________ Quadrant: _______   │
│ Name: _______________ Role: _______________ Quadrant: _______   │
│ Name: _______________ Role: _______________ Quadrant: _______   │
│ Name: _______________ Role: _______________ Quadrant: _______   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 9.2 Permission Matrix

```
┌─────────────────────────────────────────────────────────────────┐
│                    PERMISSION MATRIX                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ ENTITY: ________________________                                │
│                                                                  │
│ ┌─────────┬────────┬────────┬────────┬────────┬────────┬──────┐ │
│ │ ACTION  │ Admin  │Manager │ User   │ Viewer │ Guest  │Notes │ │
│ ├─────────┼────────┼────────┼────────┼────────┼────────┼──────┤ │
│ │ Create  │   Y    │   Y    │   C    │   N    │   N    │      │ │
│ │ Read    │   Y    │   Y    │   O    │   O    │   N    │      │ │
│ │ Update  │   Y    │   Y    │   O    │   N    │   N    │      │ │
│ │ Delete  │   Y    │   N    │   N    │   N    │   N    │      │ │
│ │ Approve │   Y    │   Y    │   N    │   N    │   N    │      │ │
│ │ Export  │   Y    │   Y    │   Y    │   Y    │   N    │      │ │
│ │ Import  │   Y    │   Y    │   N    │   N    │   N    │      │ │
│ └─────────┴────────┴────────┴────────┴────────┴────────┴──────┘ │
│                                                                  │
│ LEGEND: Y = Yes, N = No, C = Conditional, O = Own data only     │
│                                                                  │
│ CONDITIONAL RULES:                                               │
│ • C1: _________________________________________________________│
│ • C2: _________________________________________________________│
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 9.3 Navigation Flow Template

```
┌─────────────────────────────────────────────────────────────────┐
│                    NAVIGATION FLOW                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│                      ┌─────────┐                                │
│                      │  Login  │                                │
│                      └────┬────┘                                │
│                           │                                      │
│              ┌────────────┼────────────┐                        │
│              │            │            │                        │
│              ▼            ▼            ▼                        │
│         ┌─────────┐ ┌─────────┐ ┌─────────┐                    │
│         │ Admin   │ │ User    │ │ Viewer  │                    │
│         │ Dashboard│ │Dashboard│ │Dashboard│                    │
│         └────┬────┘ └────┬────┘ └────┬────┘                    │
│              │            │            │                        │
│              ▼            ▼            ▼                        │
│         ┌─────────┐ ┌─────────┐ ┌─────────┐                    │
│         │         │ │         │ │         │                    │
│         │ [Pages  │ │ [Pages  │ │ [Pages  │                    │
│         │  Admin  │ │  User   │ │ Viewer  │                    │
│         │  sees]  │ │  sees]  │ │  sees]  │                    │
│         │         │ │         │ │         │                    │
│         └─────────┘ └─────────┘ └─────────┘                    │
│                                                                  │
│ GLOBAL NAVIGATION (visible to all):                             │
│ • _____________________________________________________________│
│ • _____________________________________________________________│
│                                                                  │
│ CONTEXTUAL NAVIGATION (depends on current page):                │
│ • _____________________________________________________________│
│ • _____________________________________________________________│
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 10. Common Pitfalls & Solutions

### 10.1 Pitfall: Client Brings No Preparation

**Symptom:** Client arrives at Session 1 with nothing.

**Solution:**
```
1. Don't proceed unprepared - reschedule if needed
2. Use first 30 minutes to complete Pre-Session Questionnaire together
3. Lower expectations for this session's output
4. Emphasize homework for next session
5. Consider charging for discovery time to incentivize preparation
```

### 10.2 Pitfall: Wrong People in the Room

**Symptom:** Attendees can't answer questions. "I'll have to check with..."

**Solution:**
```
1. Before session: "Who can answer questions about [topic]?"
2. During session: Create "parking lot" for questions they can't answer
3. Schedule follow-up with right person
4. Don't make assumptions - flag as risk if unresolved
```

### 10.3 Pitfall: Scope Creep During Discovery

**Symptom:** Each session adds more requirements. Scope grows unbounded.

**Solution:**
```
1. Maintain "parking lot" for Phase 2+ ideas
2. For each new requirement, ask: "Is this MVP or later?"
3. Regularly remind: "We're defining Phase 1 right now"
4. Use MoSCoW prioritization: Must/Should/Could/Won't
5. If scope keeps growing, pause and re-align on budget/timeline
```

### 10.4 Pitfall: Design Decisions Without Decision-Maker

**Symptom:** Team makes decisions, then decision-maker overrides later.

**Solution:**
```
1. Require decision-maker at Session 1 (at minimum)
2. For any major decision without them: document as "PENDING APPROVAL"
3. Create decision log with status
4. Get explicit sign-off on decisions before proceeding
```

### 10.5 Pitfall: "Just Like [Product]" Syndrome

**Symptom:** Client wants "something like Salesforce/Amazon/etc."

**Solution:**
```
1. Ask: "Which specific features from [Product] do you want?"
2. Make them list features explicitly - don't assume
3. Research that product's feature set before session
4. Reality check: "That product cost $X million to build.
   Which 10% of features do you actually need?"
```

### 10.6 Pitfall: Technical Solutioning During Discovery

**Symptom:** Developer starts designing solutions during requirements gathering.

**Solution:**
```
1. Stay in "what" mode, not "how" mode
2. When tempted to solution: "I have ideas on how to build this,
   but first let me make sure I understand what you need."
3. Document requirements in business terms, not technical terms
4. Save architecture/design for post-discovery phase
```

### 10.7 Pitfall: Incomplete View Requirements

**Symptom:** "Simple list page" turns out to need filters, search, export, etc.

**Solution:**
```
1. Use View Design Worksheet for EVERY view
2. Ask every question even if it seems obvious
3. Show examples of complex views: "Do you need any of these features?"
4. Explicitly ask: "What can users DO on this page?"
```

### 10.8 Pitfall: Assumed Business Rules

**Symptom:** Obvious rules to client are invisible to developer.

**Solution:**
```
1. Use "What happens if..." technique for every scenario
2. Ask for edge cases: "What's the weirdest situation you've seen?"
3. Walk through concrete examples: "Show me an actual case"
4. Validate assumptions explicitly: "So it's impossible for X to happen?"
```

### 10.9 Pitfall: Mobile as Afterthought

**Symptom:** Mobile requirements only discussed when UI is built.

**Solution:**
```
1. Ask mobile questions for every view (Section 5.2.63-66)
2. Ask: "Who uses this on their phone? What do they do?"
3. If mobile matters, design mobile-first
4. Be explicit: "Is this mobile-responsive, mobile-different, or desktop-only?"
```

### 10.10 Pitfall: No Sign-Off Discipline

**Symptom:** Requirements doc exists but no formal agreement. Changes happen freely.

**Solution:**
```
1. Treat sign-off as a gate - no development until signed
2. Make sign-off a ceremony - dedicated meeting, signatures
3. After sign-off, enforce Change Request process strictly
4. Track changes and their impact visibly
5. Use changes as learning for next project's discovery
```

---

## Quick Reference: Session Checklists

### Session 1 Checklist
```
□ Introductions complete
□ Discovery process explained
□ Problem statement captured (client's words)
□ Success metrics defined
□ Stakeholder matrix complete
□ Constraints documented
□ Reference sites reviewed
□ Next session scheduled
□ Homework assigned (if any)
```

### Session 2 Checklist
```
□ All entities identified
□ Entity fields documented
□ Relationships mapped (ERD drawn)
□ Business rules captured
□ State diagrams drawn (for entities with status)
□ Workflow transitions documented
□ Approval workflows identified
□ Auto-triggers identified
```

### Session 3 Checklist
```
□ All user roles defined
□ Permission matrix complete
□ Authentication requirements documented
□ Data visibility rules clear
□ Organization/multi-tenancy understood
□ Role-specific dashboards/landing pages identified
```

### Session 4 Checklist
```
□ All pages/views inventoried
□ View Design Worksheet complete for each
□ Forms documented
□ Navigation flow mapped
□ Responsive requirements per view
□ URL/deep-linking requirements clear
```

### Session 5 Checklist
```
□ Notification requirements documented
□ Import/export requirements documented
□ Integrations listed
□ Mobile requirements clear
□ Security/compliance requirements documented
□ Timeline/phases agreed
□ Sign-off session scheduled
```

---

*Document Version: 1.0*
*Companion to Requirements Questionnaire v2.0*
*Total estimated time: 12-24 hours across all phases*
*Investment prevents 50-100+ hours of rework*
