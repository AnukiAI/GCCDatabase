# -*- coding: utf-8 -*-
"""
Parses ../guide.md into the block list both renderers consume.

Supported markdown — deliberately a small, predictable subset, so a typo produces a clear error
rather than a silently mis-rendered document:

    ---                       front matter: simple `key: value` lines, one per line
    key: value
    ---

    # 1. Section title        -> h1   (the TOC is derived from these; never written by hand)
    ## Sub-heading            -> h2
    Plain paragraph text      -> p    (blank-line separated; **bold** works anywhere)

    1. First step             -> steps  (any ordered list; numbering is re-generated)
    2. Second step

    | A | B |                 -> table  (column widths are computed from content)
    |---|---|
    | 1 | 2 |

    > [!NOTE] Label           -> callout   NOTE | WARNING | CAUTION
    > body text                            (mapped to note / warn / stop)

    ![Caption](name)          -> figure, from shots/name.jpg

    <!-- pagebreak -->        -> page break (Word); print-only break (web)

    <!-- include: name.md --> -> splices in _shared/name.md at this point, parsed with the same
                                 rules. For material that must read identically in several guides
                                 (the glossary, the permissions matrix), so a shared table cannot
                                 say different things in different documents. Includes do not nest.

Anything unrecognised raises, rather than being dropped — a guide that silently loses a paragraph
is worse than one that fails to build.
"""
import os
import re

CALLOUT_KIND = {"NOTE": "note", "WARNING": "warn", "CAUTION": "stop",
                "TIP": "note", "IMPORTANT": "warn"}


class GuideError(Exception):
    pass


def _front_matter(lines):
    meta = {}
    if not lines or lines[0].strip() != "---":
        raise GuideError("guide.md must start with a '---' front-matter block")
    i = 1
    while i < len(lines) and lines[i].strip() != "---":
        line = lines[i]
        if line.strip():
            if ":" not in line:
                raise GuideError(f"front matter line {i+1} is not 'key: value': {line!r}")
            k, v = line.split(":", 1)
            meta[k.strip()] = v.strip()
        i += 1
    if i >= len(lines):
        raise GuideError("front matter is not closed with '---'")
    return meta, i + 1


def _table_widths(headers, rows, total=6.0, min_w=0.8):
    """Word needs explicit widths. Derive them from content length instead of hand-maintaining."""
    n = len(headers)
    weight = []
    for c in range(n):
        cells = [headers[c]] + [r[c] for r in rows if c < len(r)]
        avg = sum(len(re.sub(r"\*\*", "", str(x))) for x in cells) / max(len(cells), 1)
        weight.append(max(avg, 1.0))
    scale = (total - min_w * n) / sum(weight) if sum(weight) else 0
    return [round(min_w + w * scale, 2) for w in weight]


SHARED = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_shared")
_INCLUDE_RE = re.compile(r"^<!--\s*include:\s*([A-Za-z0-9_.-]+)\s*-->$")


def _expand_includes(lines, source_path, depth=0):
    """Splice `<!-- include: name.md -->` with the contents of _shared/name.md.

    Runs on the raw line list BEFORE parsing, so an included file uses exactly the same markdown
    dialect and produces exactly the same blocks as inline text — there is no second code path to
    keep in step. A missing file raises rather than rendering an empty section: a guide silently
    missing its glossary is precisely the failure this parser exists to prevent.
    """
    out = []
    for n, line in enumerate(lines):
        m = _INCLUDE_RE.match(line.strip())
        if not m:
            out.append(line)
            continue
        if depth:
            raise GuideError(f"{source_path} line {n+1}: includes do not nest")
        target = os.path.join(SHARED, m.group(1))
        if not os.path.exists(target):
            raise GuideError(f"{source_path} line {n+1}: include not found — {target}")
        with open(target, encoding="utf-8") as fh:
            body = fh.read().replace("\r\n", "\n").split("\n")
        if body and body[0].strip() == "---":
            raise GuideError(f"{target}: a shared include must not carry front matter")
        out.extend(_expand_includes(body, target, depth + 1))
        out.append("")
    return out


def parse(path):
    raw = open(path, encoding="utf-8").read().replace("\r\n", "\n")
    lines = raw.split("\n")
    meta, i = _front_matter(lines)
    # Expanded AFTER the front matter is consumed, so `i` still indexes the original list.
    lines = lines[:i] + _expand_includes(lines[i:], path)

    blocks = []
    para_buf = []

    def flush():
        if para_buf:
            text = " ".join(x.strip() for x in para_buf).strip()
            if text:
                blocks.append(("p", text))
            para_buf.clear()

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        if not stripped:
            flush(); i += 1; continue

        if stripped.startswith("<!--"):
            flush()
            if "pagebreak" in stripped:
                blocks.append(("pagebreak", None))
            i += 1; continue

        if stripped.startswith("## "):
            flush(); blocks.append(("h2", stripped[3:].strip())); i += 1; continue

        if stripped.startswith("# "):
            flush(); blocks.append(("h1", stripped[2:].strip())); i += 1; continue

        # figure:  ![caption](name)
        m = re.match(r"^!\[(.*)\]\(([^)]+)\)$", stripped)
        if m:
            flush()
            blocks.append(("fig", (m.group(2).strip(), m.group(1).strip())))
            i += 1; continue

        # callout:  > [!NOTE] Label  /  > body...
        if stripped.startswith(">"):
            flush()
            head = re.match(r"^>\s*\[!(\w+)\]\s*(.*)$", stripped)
            if not head:
                raise GuideError(f"line {i+1}: blockquote must start '> [!NOTE] Label' — got {stripped!r}")
            kind = CALLOUT_KIND.get(head.group(1).upper())
            if not kind:
                raise GuideError(f"line {i+1}: unknown callout type {head.group(1)!r}")
            label = head.group(2).strip()
            body = []
            i += 1
            while i < len(lines) and lines[i].strip().startswith(">"):
                body.append(lines[i].strip().lstrip(">").strip())
                i += 1
            blocks.append(("callout", (kind, label, " ".join(body).strip())))
            continue

        # table
        if stripped.startswith("|"):
            flush()
            rows_raw = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                rows_raw.append([c.strip() for c in lines[i].strip().strip("|").split("|")])
                i += 1
            if len(rows_raw) < 2 or not re.match(r"^:?-{2,}:?$", rows_raw[1][0].strip()):
                raise GuideError(f"table near line {i}: missing the |---|---| separator row")
            headers, body = rows_raw[0], rows_raw[2:]
            blocks.append(("table", (headers, body, _table_widths(headers, body))))
            continue

        # ordered list -> steps
        if re.match(r"^\d+[.)]\s+", stripped):
            flush()
            items = []
            while i < len(lines) and re.match(r"^\d+[.)]\s+", lines[i].strip()):
                items.append(re.sub(r"^\d+[.)]\s+", "", lines[i].strip()))
                i += 1
            blocks.append(("steps", items))
            continue

        if stripped.startswith(("- ", "* ")):
            raise GuideError(f"line {i+1}: bullet lists are not supported — use a numbered list "
                             f"or a table")

        para_buf.append(line)
        i += 1

    flush()

    # TOC derived from the h1s, so it cannot fall out of step with the sections
    toc = [re.sub(r"^\d+\.\s*", "", t) for k, t in blocks if k == "h1"]

    META = {
        "org": meta.get("org", ""),
        "title": meta.get("title", "Guide"),
        "lede": meta.get("lede", ""),
        "colophon": meta.get("colophon", ""),
        "toc": toc,
        "rows": [("Version", meta.get("version", "")),
                 ("Issued", meta.get("issued", "")),
                 ("Applies to", meta.get("applies_to", "")),
                 ("Prepared by", meta.get("prepared_by", "")),
                 ("Audience", meta.get("audience", ""))],
    }

    # A leading callout before the first heading becomes the cover note.
    intro = None
    if blocks and blocks[0][0] == "callout":
        intro = blocks.pop(0)[1]
    META["intro_callout"] = intro

    return META, blocks


if __name__ == "__main__":
    m, b = parse(os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "guide.md"))
    from collections import Counter
    print("title    :", m["title"])
    print("sections :", len(m["toc"]))
    print("blocks   :", dict(Counter(k for k, _ in b)))
