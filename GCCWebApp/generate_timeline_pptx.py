"""
Generate Amazon API Integration Timeline PowerPoint — UPDATED
Removed SP-API Push scope. Amazon pulls from our API.
"""

from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE
import os

# ── Colors ──
GCC_GREEN_DARK = RGBColor(0x1B, 0x5E, 0x20)
GCC_GREEN = RGBColor(0x2E, 0x7D, 0x32)
GCC_GREEN_LIGHT = RGBColor(0x4C, 0xAF, 0x50)
GCC_GREEN_BG = RGBColor(0xE8, 0xF5, 0xE9)
ACCENT_BLUE = RGBColor(0x15, 0x65, 0xC0)
ACCENT_BLUE_LIGHT = RGBColor(0xE3, 0xF2, 0xFD)
ORANGE = RGBColor(0xE6, 0x51, 0x00)
ORANGE_LIGHT = RGBColor(0xFF, 0xF3, 0xE0)
RED = RGBColor(0xC6, 0x28, 0x28)
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
BLACK = RGBColor(0x21, 0x21, 0x21)
GRAY_700 = RGBColor(0x61, 0x61, 0x61)
GRAY_600 = RGBColor(0x75, 0x75, 0x75)
GRAY_300 = RGBColor(0xE0, 0xE0, 0xE0)
GRAY_200 = RGBColor(0xEE, 0xEE, 0xEE)
GRAY_100 = RGBColor(0xF5, 0xF5, 0xF5)

prs = Presentation()
prs.slide_width = Inches(13.333)
prs.slide_height = Inches(7.5)
SLIDE_W = Inches(13.333)
SLIDE_H = Inches(7.5)


def add_shape(slide, left, top, width, height, fill_color=None):
    shape = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, top, width, height)
    shape.line.fill.background()
    if fill_color:
        shape.fill.solid()
        shape.fill.fore_color.rgb = fill_color
    else:
        shape.fill.background()
    return shape


def add_rounded(slide, left, top, width, height, fill_color=None):
    shape = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
    shape.line.fill.background()
    if fill_color:
        shape.fill.solid()
        shape.fill.fore_color.rgb = fill_color
    return shape


def add_text(slide, left, top, width, height, text, size=14, bold=False, color=BLACK, align=PP_ALIGN.LEFT):
    txbox = slide.shapes.add_textbox(left, top, width, height)
    txbox.text_frame.word_wrap = True
    p = txbox.text_frame.paragraphs[0]
    p.text = text
    p.font.size = Pt(size)
    p.font.bold = bold
    p.font.color.rgb = color
    p.font.name = 'Segoe UI'
    p.alignment = align
    return txbox


def add_lines(slide, left, top, width, height, lines, size=14, color=BLACK):
    txbox = slide.shapes.add_textbox(left, top, width, height)
    tf = txbox.text_frame
    tf.word_wrap = True
    for i, (txt, bld, clr) in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.text = txt
        p.font.size = Pt(size)
        p.font.bold = bld
        p.font.color.rgb = clr or color
        p.font.name = 'Segoe UI'
        p.space_after = Pt(2)
    return txbox


def add_table(slide, left, top, width, rows_data, col_widths, hdr_bg=GRAY_100, hdr_color=BLACK, fsize=11, rh=Inches(0.38)):
    cols = len(col_widths)
    rows = len(rows_data)
    shape = slide.shapes.add_table(rows, cols, left, top, width, rh * rows)
    tbl = shape.table
    for ci, w in enumerate(col_widths):
        tbl.columns[ci].width = w
    for ri, row in enumerate(rows_data):
        for ci, cd in enumerate(row):
            cell = tbl.cell(ri, ci)
            cell.vertical_anchor = MSO_ANCHOR.MIDDLE
            if isinstance(cd, dict):
                text = cd.get('text', '')
                cb = cd.get('bold', ri == 0)
                cc = cd.get('color', hdr_color if ri == 0 else BLACK)
                cbg = cd.get('bg', None)
                ca = cd.get('align', PP_ALIGN.LEFT)
            else:
                text, cb, cc, cbg, ca = str(cd), ri == 0, hdr_color if ri == 0 else BLACK, None, PP_ALIGN.LEFT
            p = cell.text_frame.paragraphs[0]
            p.text = text
            p.font.size = Pt(fsize)
            p.font.bold = cb
            p.font.color.rgb = cc
            p.font.name = 'Segoe UI'
            p.alignment = ca
            if ri == 0:
                cell.fill.solid()
                cell.fill.fore_color.rgb = hdr_bg
            elif cbg:
                cell.fill.solid()
                cell.fill.fore_color.rgb = cbg
            else:
                cell.fill.background()
    return shape


# ════════════════════════════════════════════════════════════════
# SLIDE 1 — TITLE
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, SLIDE_H, GCC_GREEN_DARK)
add_shape(s, Inches(0), Inches(0), Inches(0.15), SLIDE_H, GCC_GREEN_LIGHT)
add_text(s, Inches(1.2), Inches(1.2), Inches(10), Inches(0.5), 'ANUKI CONSULTING', size=14, color=RGBColor(0xA5,0xD6,0xA7))
add_text(s, Inches(1.2), Inches(2.2), Inches(10), Inches(1.2), 'Amazon API Integration Timeline', size=40, bold=True, color=WHITE)
add_text(s, Inches(1.2), Inches(3.6), Inches(10), Inches(0.6), 'GCC Database Solution \u2014 Milestone Schedule & Risk Assessment', size=20, color=RGBColor(0xC8,0xE6,0xC9))
add_shape(s, Inches(1.2), Inches(4.6), Inches(4), Inches(0.03), RGBColor(0xA5,0xD6,0xA7))
add_text(s, Inches(1.2), Inches(5.0), Inches(10), Inches(0.4), 'Prepared for GreenCircle Certified', size=16, color=RGBColor(0xA5,0xD6,0xA7))
add_text(s, Inches(1.2), Inches(5.5), Inches(10), Inches(0.4), 'April 15, 2026', size=16, color=RGBColor(0xA5,0xD6,0xA7))
add_text(s, Inches(1.2), Inches(6.5), Inches(10), Inches(0.3), 'CONFIDENTIAL', size=11, color=RGBColor(0x81,0xC7,0x84))


# ════════════════════════════════════════════════════════════════
# SLIDE 2 — EXECUTIVE SUMMARY
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), GCC_GREEN_DARK)
add_text(s, Inches(0.8), Inches(0.2), Inches(10), Inches(0.5), 'Executive Summary', size=26, bold=True, color=WHITE)

add_rounded(s, Inches(0.8), Inches(1.3), Inches(11.7), Inches(2.0), GCC_GREEN_BG)
add_shape(s, Inches(0.8), Inches(1.3), Inches(0.12), Inches(2.0), GCC_GREEN)
add_lines(s, Inches(1.2), Inches(1.45), Inches(11), Inches(1.8), [
    ('Amazon has requested firm milestone dates for API integration and automated product badging.', False, GRAY_700),
    ('', False, None),
    ('This document presents two timeline options with dependencies, risk factors, and mitigation strategies.', False, GRAY_700),
    ('Both options are achievable contingent on completing the current UAT cycle without significant new scope.', True, BLACK),
], size=15)

for i, (title, desc) in enumerate([
    ('API Infrastructure', 'Endpoints, authentication, and GS1 data\nschema are already built and tested.\nAmazon pulls data from our API.'),
    ('Database Stabilization', '40 items in final UAT retest. Must\ncomplete before API schema lock.'),
    ('Integration Testing', 'Validate with Amazon sandbox,\nload test, security review,\nand schema confirmation.'),
]):
    x = Inches(0.8 + i * 4.0)
    add_rounded(s, x, Inches(3.8), Inches(3.6), Inches(2.4), GRAY_100)
    add_shape(s, x, Inches(3.8), Inches(3.6), Inches(0.06), GCC_GREEN)
    add_text(s, x + Inches(0.3), Inches(4.1), Inches(3.0), Inches(0.4), title, size=16, bold=True, color=GCC_GREEN_DARK)
    add_text(s, x + Inches(0.3), Inches(4.6), Inches(3.0), Inches(1.2), desc, size=13, color=GRAY_700)


# ════════════════════════════════════════════════════════════════
# SLIDE 3 — CURRENT STATE OF READINESS
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), GCC_GREEN_DARK)
add_text(s, Inches(0.8), Inches(0.2), Inches(10), Inches(0.5), 'Current State of Readiness', size=26, bold=True, color=WHITE)

rows = [
    ['Component', 'Status', 'Details'],
    ['Certificate Database Application', {'text': '95% Complete', 'color': ORANGE, 'bold': True}, '40 items in final UAT retest cycle'],
    ['REST API Endpoints', {'text': 'Built', 'color': GCC_GREEN, 'bold': True}, '/api/v1/certificates, search by ASIN, GS1 JSON-LD'],
    ['API Authentication', {'text': 'Built', 'color': GCC_GREEN, 'bold': True}, 'Partner API keys with rate limiting (200 req/hr)'],
    ['Amazon Developer Guide', {'text': 'CP2 Complete', 'color': GCC_GREEN, 'bold': True}, 'Reviewed by GCC; feedback incorporated'],
    ['GS1 Data Schema (JSON-LD)', {'text': 'Built', 'color': GCC_GREEN, 'bold': True}, 'JSON-LD format per GS1 Web Vocabulary standard'],
    ['Search by Product ID (ASIN)', {'text': 'Built', 'color': GCC_GREEN, 'bold': True}, 'GET /api/v1/certificates/search/productid?type=ASIN'],
    ['Partner-Scoped Data Filtering', {'text': 'Built', 'color': GCC_GREEN, 'bold': True}, 'API responses filtered by partner association'],
]
add_table(s, Inches(0.8), Inches(1.3), Inches(11.7), rows,
          [Inches(3.5), Inches(1.8), Inches(6.4)],
          hdr_bg=GCC_GREEN_DARK, hdr_color=WHITE, fsize=12, rh=Inches(0.48))

add_rounded(s, Inches(0.8), Inches(5.6), Inches(11.7), Inches(1.2), GCC_GREEN_BG)
add_shape(s, Inches(0.8), Inches(5.6), Inches(0.1), Inches(1.2), GCC_GREEN)
add_lines(s, Inches(1.2), Inches(5.7), Inches(11), Inches(1.0), [
    ('Integration Model: Amazon pulls certificate data from our API.', True, GCC_GREEN_DARK),
    ('All API endpoints, authentication, data schema, and partner filtering are built and operational.', False, GRAY_700),
    ('Remaining work is database stabilization (UAT), schema confirmation with Amazon, and integration testing.', False, GRAY_700),
], size=13)


# ════════════════════════════════════════════════════════════════
# SLIDE 4 — OPTION A
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), ACCENT_BLUE)
add_text(s, Inches(0.8), Inches(0.2), Inches(10), Inches(0.5),
         'Option A \u2014 Aggressive Timeline (Amazon\u2019s Requested Dates)', size=24, bold=True, color=WHITE)

rows = [
    [{'text': 'Phase', 'bold': True}, {'text': 'Milestone', 'bold': True}, {'text': 'Dates', 'bold': True}, {'text': 'Duration', 'bold': True}],
    ['Phase 1', 'Database Stabilization & UAT Retest', 'Apr 15 \u2014 May 7', '3 weeks'],
    ['Phase 2', 'API Schema Finalization & Lock', 'May 8 \u2014 May 31', '3.5 weeks'],
    [{'text': '\u2605', 'color': ORANGE, 'bold': True, 'align': PP_ALIGN.CENTER},
     {'text': 'AMAZON MILESTONE 1 \u2014 May 31, 2026: Finalized API Endpoint & Data Schema', 'bold': True, 'color': ORANGE, 'bg': ORANGE_LIGHT},
     {'text': '', 'bg': ORANGE_LIGHT}, {'text': '', 'bg': ORANGE_LIGHT}],
    ['Phase 3', 'Integration Testing with Amazon Sandbox', 'Jun 1 \u2014 Jul 15', '6 weeks'],
    ['Phase 4', 'Final QA & Production Hardening', 'Jul 16 \u2014 Jul 31', '2 weeks'],
    [{'text': '\u2605', 'color': ORANGE, 'bold': True, 'align': PP_ALIGN.CENTER},
     {'text': 'AMAZON MILESTONE 2 \u2014 July 31, 2026: API Technical Implementation & Testing Complete', 'bold': True, 'color': ORANGE, 'bg': ORANGE_LIGHT},
     {'text': '', 'bg': ORANGE_LIGHT}, {'text': '', 'bg': ORANGE_LIGHT}],
    ['Phase 5', 'Automated Badging Go-Live', 'Aug 1 \u2014 Aug 31', 'Rollout'],
    [{'text': '\u2605', 'color': ORANGE, 'bold': True, 'align': PP_ALIGN.CENTER},
     {'text': 'AMAZON MILESTONE 3 \u2014 August 2026: Automated Badging Live', 'bold': True, 'color': ORANGE, 'bg': ORANGE_LIGHT},
     {'text': '', 'bg': ORANGE_LIGHT}, {'text': '', 'bg': ORANGE_LIGHT}],
]
add_table(s, Inches(0.8), Inches(1.2), Inches(11.7), rows,
          [Inches(1.2), Inches(7.0), Inches(2.2), Inches(1.3)],
          hdr_bg=ACCENT_BLUE, hdr_color=WHITE, fsize=12, rh=Inches(0.44))

add_rounded(s, Inches(0.8), Inches(5.6), Inches(11.7), Inches(1.5), ACCENT_BLUE_LIGHT)
add_shape(s, Inches(0.8), Inches(5.6), Inches(0.1), Inches(1.5), ACCENT_BLUE)
add_lines(s, Inches(1.2), Inches(5.7), Inches(11), Inches(1.3), [
    ('Option A is achievable provided:', True, ACCENT_BLUE),
    ('\u2022  GCC commits to completing UAT retest by May 7 with dedicated testing time', False, GRAY_700),
    ('\u2022  No new feature requests are introduced during the stabilization phase', False, GRAY_700),
    ('\u2022  All 5 pending discussion items are resolved by May 1', False, GRAY_700),
    ('\u2022  Amazon sandbox credentials are available by June 1', False, GRAY_700),
], size=13)


# ════════════════════════════════════════════════════════════════
# SLIDE 5 — OPTION A PHASE DETAILS
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), ACCENT_BLUE)
add_text(s, Inches(0.8), Inches(0.2), Inches(10), Inches(0.5), 'Option A \u2014 Phase Details', size=24, bold=True, color=WHITE)

phases = [
    ('Phase 1: DB Stabilization (Apr 15 \u2014 May 7)',
     'Deploy code fixes (24 files) + 4 SQL migration scripts\nClient UAT retest of 40+ resolved items\nResolve retest failures\nResolve 5 items pending client decision\nData cleanup (test entries, hierarchy fixes)\n\nExit Criteria: All critical/high QC items confirmed fixed'),
    ('Phase 2: API Schema Lock (May 8 \u2014 May 31)',
     'Confirm GS1 JSON-LD schema meets Amazon CPF requirements\nDocument final field mappings (ASIN-only per Amazon)\nLock API contract \u2014 no breaking changes after this date\nPublish final API documentation for Amazon\nProduction deployment of stabilized database'),
    ('Phase 3: Integration Testing (Jun 1 \u2014 Jul 15)',
     'End-to-end testing with Amazon sandbox environment\nValidate Amazon can pull cert data via our API\nVerify ASIN search, GS1 format, partner filtering\nLoad testing and performance validation\nSecurity review of API authentication'),
    ('Phase 4: Final QA & Go-Live (Jul 16 \u2014 Aug 31)',
     'Final QA pass with Amazon technical team\nProduction hardening and monitoring setup\nCoordinate go-live with Amazon\nMonitor initial automated badge cycles\nTransition to steady-state operations'),
]

for i, (title, desc) in enumerate(phases):
    col, row = i % 2, i // 2
    x, y = Inches(0.8 + col * 6.1), Inches(1.2 + row * 3.0)
    add_rounded(s, x, y, Inches(5.8), Inches(2.7), GRAY_100)
    add_shape(s, x, y, Inches(5.8), Inches(0.06), ACCENT_BLUE)
    add_text(s, x + Inches(0.3), y + Inches(0.2), Inches(5.2), Inches(0.4), title, size=13, bold=True, color=ACCENT_BLUE)
    add_text(s, x + Inches(0.3), y + Inches(0.65), Inches(5.2), Inches(1.9), desc, size=11, color=GRAY_700)


# ════════════════════════════════════════════════════════════════
# SLIDE 6 — OPTION B
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), GCC_GREEN_DARK)
add_text(s, Inches(0.8), Inches(0.2), Inches(11), Inches(0.5),
         'Option B \u2014 Buffered Timeline (+1 Month, End of Q3 2026)', size=24, bold=True, color=WHITE)

rows = [
    [{'text': 'Phase', 'bold': True}, {'text': 'Milestone', 'bold': True}, {'text': 'Dates', 'bold': True}, {'text': 'Duration', 'bold': True}],
    ['Phase 1', 'Database Stabilization & UAT Retest', 'Apr 15 \u2014 May 31', '6 weeks'],
    ['Phase 2', 'API Schema Finalization & Lock', 'Jun 1 \u2014 Jun 30', '4 weeks'],
    [{'text': '\u2605', 'color': ORANGE, 'bold': True, 'align': PP_ALIGN.CENTER},
     {'text': 'AMAZON MILESTONE 1 \u2014 June 30, 2026: Finalized API Endpoint & Data Schema', 'bold': True, 'color': ORANGE, 'bg': ORANGE_LIGHT},
     {'text': '', 'bg': ORANGE_LIGHT}, {'text': '', 'bg': ORANGE_LIGHT}],
    ['Phase 3', 'Integration Testing with Amazon Sandbox', 'Jul 1 \u2014 Aug 15', '6 weeks'],
    ['Phase 4', 'Final QA & Production Hardening', 'Aug 16 \u2014 Aug 31', '2 weeks'],
    [{'text': '\u2605', 'color': ORANGE, 'bold': True, 'align': PP_ALIGN.CENTER},
     {'text': 'AMAZON MILESTONE 2 \u2014 August 31, 2026: API Technical Implementation & Testing Complete', 'bold': True, 'color': ORANGE, 'bg': ORANGE_LIGHT},
     {'text': '', 'bg': ORANGE_LIGHT}, {'text': '', 'bg': ORANGE_LIGHT}],
    ['Phase 5', 'Automated Badging Go-Live', 'Sep 1 \u2014 Sep 30', 'Rollout'],
    [{'text': '\u2605', 'color': ORANGE, 'bold': True, 'align': PP_ALIGN.CENTER},
     {'text': 'AMAZON MILESTONE 3 \u2014 September 2026: Automated Badging Live', 'bold': True, 'color': ORANGE, 'bg': ORANGE_LIGHT},
     {'text': '', 'bg': ORANGE_LIGHT}, {'text': '', 'bg': ORANGE_LIGHT}],
]
add_table(s, Inches(0.8), Inches(1.2), Inches(11.7), rows,
          [Inches(1.2), Inches(7.0), Inches(2.2), Inches(1.3)],
          hdr_bg=GCC_GREEN_DARK, hdr_color=WHITE, fsize=12, rh=Inches(0.44))

add_rounded(s, Inches(0.8), Inches(5.6), Inches(11.7), Inches(1.5), GCC_GREEN_BG)
add_shape(s, Inches(0.8), Inches(5.6), Inches(0.1), Inches(1.5), GCC_GREEN)
add_lines(s, Inches(1.2), Inches(5.7), Inches(11), Inches(1.3), [
    ('Key Differences from Option A:', True, GCC_GREEN_DARK),
    ('\u2022  +3 weeks UAT buffer \u2014 accounts for historically observed 15\u201330 new items per retest cycle', False, GRAY_700),
    ('\u2022  +2 weeks schema review \u2014 allows iteration with Amazon technical team on CPF requirements', False, GRAY_700),
    ('\u2022  Same testing time \u2014 integration testing (6 weeks) and final QA (2 weeks) unchanged', False, GRAY_700),
    ('\u2022  End of Q3 alignment \u2014 completes within a natural reporting boundary', False, GRAY_700),
], size=13)


# ════════════════════════════════════════════════════════════════
# SLIDE 7 — SIDE-BY-SIDE COMPARISON
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), GCC_GREEN_DARK)
add_text(s, Inches(0.8), Inches(0.2), Inches(10), Inches(0.5), 'Side-by-Side Comparison', size=26, bold=True, color=WHITE)

rows = [
    [{'text': 'Milestone', 'bold': True},
     {'text': 'Option A (Aggressive)', 'bold': True, 'align': PP_ALIGN.CENTER},
     {'text': 'Option B (Buffered)', 'bold': True, 'align': PP_ALIGN.CENTER},
     {'text': 'Delta', 'bold': True, 'align': PP_ALIGN.CENTER}],
    [{'text': 'API Schema Finalized', 'bold': True},
     {'text': 'May 31, 2026', 'align': PP_ALIGN.CENTER, 'bg': ACCENT_BLUE_LIGHT, 'bold': True},
     {'text': 'June 30, 2026', 'align': PP_ALIGN.CENTER, 'bg': GCC_GREEN_BG, 'bold': True},
     {'text': '+1 month', 'align': PP_ALIGN.CENTER, 'bold': True, 'color': GRAY_600}],
    [{'text': 'API Implementation & Testing', 'bold': True},
     {'text': 'July 31, 2026', 'align': PP_ALIGN.CENTER, 'bg': ACCENT_BLUE_LIGHT, 'bold': True},
     {'text': 'August 31, 2026', 'align': PP_ALIGN.CENTER, 'bg': GCC_GREEN_BG, 'bold': True},
     {'text': '+1 month', 'align': PP_ALIGN.CENTER, 'bold': True, 'color': GRAY_600}],
    [{'text': 'Automated Badging Go-Live', 'bold': True},
     {'text': 'August 2026', 'align': PP_ALIGN.CENTER, 'bg': ACCENT_BLUE_LIGHT, 'bold': True},
     {'text': 'September 2026', 'align': PP_ALIGN.CENTER, 'bg': GCC_GREEN_BG, 'bold': True},
     {'text': '+1 month', 'align': PP_ALIGN.CENTER, 'bold': True, 'color': GRAY_600}],
    ['', '', '', ''],
    ['UAT Stabilization Window',
     {'text': '3 weeks', 'align': PP_ALIGN.CENTER},
     {'text': '6 weeks', 'align': PP_ALIGN.CENTER},
     {'text': '+3 weeks', 'align': PP_ALIGN.CENTER, 'color': GRAY_600}],
    ['Integration Testing Time',
     {'text': '6 weeks', 'align': PP_ALIGN.CENTER},
     {'text': '6 weeks', 'align': PP_ALIGN.CENTER},
     {'text': 'Same', 'align': PP_ALIGN.CENTER, 'color': GCC_GREEN}],
    ['New API Development Required',
     {'text': 'None', 'align': PP_ALIGN.CENTER, 'color': GCC_GREEN, 'bold': True},
     {'text': 'None', 'align': PP_ALIGN.CENTER, 'color': GCC_GREEN, 'bold': True},
     {'text': 'API is built', 'align': PP_ALIGN.CENTER, 'color': GCC_GREEN, 'bold': True}],
]
add_table(s, Inches(1.5), Inches(1.4), Inches(10.3), rows,
          [Inches(3.2), Inches(2.5), Inches(2.5), Inches(2.1)],
          hdr_bg=GCC_GREEN_DARK, hdr_color=WHITE, fsize=13, rh=Inches(0.52))

add_text(s, Inches(1.5), Inches(6.0), Inches(10.3), Inches(0.8),
         'The API is fully built. No new development is required.\nThe remaining effort is database stabilization, schema confirmation, and integration testing.',
         size=14, color=GRAY_600, align=PP_ALIGN.CENTER)


# ════════════════════════════════════════════════════════════════
# SLIDE 8 — RISK FACTORS
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), GCC_GREEN_DARK)
add_text(s, Inches(0.8), Inches(0.2), Inches(10), Inches(0.5), 'Schedule Risk Factors', size=26, bold=True, color=WHITE)

rows = [
    [{'text': 'Risk', 'bold': True}, {'text': 'Impact', 'bold': True},
     {'text': 'Likelihood', 'bold': True}, {'text': 'Mitigation', 'bold': True}],
    [{'text': 'UAT retest generates significant new defects', 'bold': True},
     'Delays Phase 1; cascades to all milestones',
     {'text': 'Medium-High', 'color': ORANGE, 'bold': True},
     'Feature freeze after Phase 1. Only bug fixes, no new scope.'],
    [{'text': 'New feature requests during UAT', 'bold': True},
     'Scope creep delays stabilization',
     {'text': 'Medium', 'color': ORANGE, 'bold': True},
     'All new requests treated as formal Change Requests with separate timeline.'],
    [{'text': 'Amazon schema requirements change after review', 'bold': True},
     'Rework on finalized API contract',
     {'text': 'Medium', 'color': ORANGE, 'bold': True},
     'Early schema review with Amazon. Lock with formal sign-off.'],
    [{'text': 'Client availability for testing', 'bold': True},
     'UAT cycle extends beyond window',
     {'text': 'Medium', 'color': ORANGE, 'bold': True},
     'Detailed test cases provided. Set testing windows with committed dates.'],
    [{'text': 'Amazon sandbox access delays', 'bold': True},
     'Blocks integration testing',
     {'text': 'Low-Medium', 'color': GCC_GREEN, 'bold': True},
     'Request sandbox credentials by May 15 regardless of timeline option.'],
]
add_table(s, Inches(0.8), Inches(1.2), Inches(11.7), rows,
          [Inches(3.0), Inches(2.5), Inches(1.5), Inches(4.7)],
          hdr_bg=GCC_GREEN_DARK, hdr_color=WHITE, fsize=11, rh=Inches(0.7))


# ════════════════════════════════════════════════════════════════
# SLIDE 9 — EXTERNAL DEPENDENCIES
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), GCC_GREEN_DARK)
add_text(s, Inches(0.8), Inches(0.2), Inches(10), Inches(0.5), 'External Dependencies', size=26, bold=True, color=WHITE)
add_text(s, Inches(0.8), Inches(1.1), Inches(11), Inches(0.4),
         'The following items are outside of Anuki\u2019s direct control and must be completed by the indicated parties.', size=13, color=GRAY_600)

rows = [
    [{'text': 'Dependency', 'bold': True}, {'text': 'Owner', 'bold': True},
     {'text': 'Needed By', 'bold': True}, {'text': 'Status', 'bold': True}],
    ['Complete UAT retest of 40 open items', {'text': 'GCC (Carly)', 'bold': True}, 'May 7 (A) / May 31 (B)', {'text': 'Not Started', 'color': RED, 'bold': True}],
    ['Decisions on 5 pending discussion items', {'text': 'GCC (Carly / Dan)', 'bold': True}, 'May 1', {'text': 'Pending', 'color': ORANGE, 'bold': True}],
    ['Amazon SP-API sandbox credentials', {'text': 'GCC / Amazon', 'bold': True}, 'Jun 1', {'text': 'Not Requested', 'color': RED, 'bold': True}],
    ['Amazon technical team schema review', {'text': 'Amazon', 'bold': True}, 'Jun 15', {'text': 'Not Scheduled', 'color': RED, 'bold': True}],
    ['Final production URL / DNS configuration', {'text': 'GCC', 'bold': True}, 'Jul 1', {'text': 'Pending', 'color': ORANGE, 'bold': True}],
    ['Real certificate data loaded in production DB', {'text': 'GCC (Analysts)', 'bold': True}, 'Jul 1', {'text': 'Partial', 'color': ORANGE, 'bold': True}],
]
add_table(s, Inches(0.8), Inches(1.6), Inches(11.7), rows,
          [Inches(4.5), Inches(2.2), Inches(2.5), Inches(2.5)],
          hdr_bg=GCC_GREEN_DARK, hdr_color=WHITE, fsize=12, rh=Inches(0.52))

add_rounded(s, Inches(0.8), Inches(5.8), Inches(11.7), Inches(1.0), ORANGE_LIGHT)
add_shape(s, Inches(0.8), Inches(5.8), Inches(0.1), Inches(1.0), ORANGE)
add_lines(s, Inches(1.2), Inches(5.9), Inches(11), Inches(0.8), [
    ('Schedule Impact: Any delay in the above dependencies directly impacts the Amazon milestone dates.', True, ORANGE),
    ('The UAT retest completion is the single largest dependency controlling the schedule.', False, GRAY_700),
], size=13)


# ════════════════════════════════════════════════════════════════
# SLIDE 10 — RECOMMENDATION
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), GCC_GREEN_DARK)
add_text(s, Inches(0.8), Inches(0.2), Inches(10), Inches(0.5), 'Recommendation', size=26, bold=True, color=WHITE)

add_rounded(s, Inches(0.8), Inches(1.3), Inches(11.7), Inches(5.5), GCC_GREEN_BG)
add_shape(s, Inches(0.8), Inches(1.3), Inches(11.7), Inches(0.08), GCC_GREEN)

add_text(s, Inches(1.4), Inches(1.6), Inches(10.5), Inches(0.5),
         'Anuki Recommends Option B (Buffered Timeline)', size=22, bold=True, color=GCC_GREEN_DARK)

reasons = [
    ('1.  Historical pattern:', 'Every UAT cycle on this project has generated 15\u201330 new items. Option A assumes a clean retest, which has not occurred in any prior cycle.'),
    ('2.  API is already built:', 'No new API development is required. Amazon pulls data from our existing endpoints. The remaining work is stabilization and testing \u2014 not construction.'),
    ('3.  Accountability protection:', 'A missed aggressive deadline damages the Amazon relationship more than a met conservative one.'),
    ('4.  Q3 alignment:', 'Option B completes by end of Q3 2026, a natural reporting boundary.'),
]

y = Inches(2.3)
for title, desc in reasons:
    add_lines(s, Inches(1.4), y, Inches(10.5), Inches(0.7), [
        (title, True, GCC_GREEN_DARK),
        (desc, False, GRAY_700),
    ], size=13)
    y += Inches(0.85)

add_shape(s, Inches(1.4), y + Inches(0.1), Inches(10.5), Inches(0.02), GCC_GREEN)
add_lines(s, Inches(1.4), y + Inches(0.25), Inches(10.5), Inches(0.8), [
    ('However, if Amazon requires the aggressive timeline, Option A is achievable', True, BLACK),
    ('provided GCC commits to completing UAT by May 7 with no new feature requests.', False, GRAY_700),
], size=14)


# ════════════════════════════════════════════════════════════════
# SLIDE 11 — NEXT STEPS
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, Inches(0.9), GCC_GREEN_DARK)
add_text(s, Inches(0.8), Inches(0.2), Inches(10), Inches(0.5), 'Immediate Next Steps', size=26, bold=True, color=WHITE)

rows = [
    [{'text': 'Action', 'bold': True}, {'text': 'Owner', 'bold': True}, {'text': 'Due Date', 'bold': True}],
    [{'text': 'Confirm timeline option with Amazon', 'bold': True}, 'GCC', {'text': 'April 15, 2026', 'bold': True, 'color': RED}],
    ['Deploy current code fixes + SQL migration scripts', 'Anuki', 'April 16, 2026'],
    ['Begin UAT retest using provided test cases (40 items)', 'GCC', 'April 17, 2026'],
    ['Resolve 5 pending discussion items', 'GCC + Anuki', 'April 30, 2026'],
    ['Request Amazon sandbox credentials', 'GCC', 'May 1, 2026'],
    ['Complete UAT retest cycle', 'GCC', 'May 7 (A) / May 31 (B)'],
]
add_table(s, Inches(1.5), Inches(1.4), Inches(10.3), rows,
          [Inches(5.5), Inches(2.2), Inches(2.6)],
          hdr_bg=GCC_GREEN_DARK, hdr_color=WHITE, fsize=14, rh=Inches(0.58))

add_text(s, Inches(1.5), Inches(6.0), Inches(10.3), Inches(0.8),
         'Questions or concerns? Contact Anuki Consulting to discuss timeline options.\nWe are committed to delivering a high-quality, reliable integration for the Amazon partnership.',
         size=14, color=GRAY_600, align=PP_ALIGN.CENTER)


# ════════════════════════════════════════════════════════════════
# SLIDE 12 — CLOSING
# ════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(prs.slide_layouts[6])
add_shape(s, Inches(0), Inches(0), SLIDE_W, SLIDE_H, GCC_GREEN_DARK)
add_shape(s, Inches(0), Inches(0), Inches(0.15), SLIDE_H, GCC_GREEN_LIGHT)
add_text(s, Inches(1.2), Inches(2.5), Inches(10), Inches(0.8), 'Thank You', size=44, bold=True, color=WHITE)
add_shape(s, Inches(1.2), Inches(3.6), Inches(3), Inches(0.03), RGBColor(0xA5,0xD6,0xA7))
add_text(s, Inches(1.2), Inches(4.0), Inches(10), Inches(0.4), 'Anuki Consulting', size=20, color=RGBColor(0xC8,0xE6,0xC9))
add_text(s, Inches(1.2), Inches(4.6), Inches(10), Inches(0.4), 'Prepared for GreenCircle Certified \u2014 April 2026', size=14, color=RGBColor(0xA5,0xD6,0xA7))
add_text(s, Inches(1.2), Inches(5.8), Inches(10), Inches(0.3), 'CONFIDENTIAL', size=11, color=RGBColor(0x81,0xC7,0x84))


# ── SAVE ──
output_path = os.path.join(
    r'C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp',
    'Amazon_API_Integration_Timeline_v2.pptx'
)
prs.save(output_path)
print(f'Saved: {output_path}')
