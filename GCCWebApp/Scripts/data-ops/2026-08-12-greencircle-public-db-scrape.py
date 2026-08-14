#!/usr/bin/env python3
"""
GreenCircle Certified certificate mirror  (public database + signed-in portal)
=============================================================================

Downloads certificate PDFs from the GreenCircle Certified database and writes a
metadata manifest (CSV + JSON) covering every record. Two modes:

  default   the PUBLIC product database -- unauthenticated, ~1,021 records, i.e.
            only the certificates whose owners consented to publish.
  --portal  the SIGNED-IN portal -- ~6,000 records across every status (Expired,
            DRAFT - Internal, Client Approved - Confidential, Hold, ...), using a
            NextAuth session cookie supplied by the operator via $GCC_PORTAL_COOKIE.

Both modes are READ-ONLY. Neither bypasses anything: the default mode calls the
same endpoints the public page calls, and --portal calls the same endpoints the
portal page calls, with a session the operator is already signed in to. The
portal exposes write procedures (certificate.sendForReview, certificate.exportCSV)
and this script deliberately calls NEITHER.

--portal is for the GCC migration PDF pull: the client's own migration plan
(row 13) records Anuki confirming a script would pull documents from the old
database to the new one. 4,599 of the certificates in the go-live load set have
no publicly available PDF, so the public mode alone cannot complete that job.

How the site works (verified 2026-08-12)
---------------------------------------
  https://db.greencirclecertified.com/certificate  is a Next.js static export.
  The Ant Design table is populated client-side from a single call:

      GET /api/certificates              -> JSON array of ALL records (no paging;
                                            the table paginates client-side)
      GET /api/certificates/{id}/download -> 307 redirect to a presigned S3 URL

  So there is no pagination to walk and no headless browser needed -- one index
  call gives the complete population, and the "total count" is simply its length.

Output layout (under --out, default ./_gcc-public-mirror)
---------------------------------------------------------
    certificates/<CertNo>_<Company>_<id>.pdf   one file per downloadable record
    manifest.csv                               one row per record, every column
    manifest.json                              same data, nested + raw fields
    api-certificates-raw.json                  untouched API response (audit trail)
    scrape-log-<timestamp>.txt                 run log
    .gitignore                                 written automatically ("*") so the
                                               mirror can never be committed

Resumability
------------
  Re-running is safe and cheap. A record is skipped when its PDF is already on
  disk and non-empty; only missing files and prior failures are retried. The
  metadata manifest is always rebuilt fresh from the API, so a re-run doubles as
  a "refresh the mirror" job. Use --force to re-download everything.

Good-citizen behaviour
----------------------
  * Real browser User-Agent, identifying Accept headers.
  * robots.txt is fetched and honoured (the site currently serves 404 = no rules;
    if rules ever appear, disallowed paths are skipped and Crawl-delay wins over
    --delay).
  * Single-threaded, with --delay seconds between requests (default 1.0).
  * Retries with exponential backoff on 429/5xx/network errors; Retry-After is
    honoured when present.

Usage
-----
  python 2026-08-12-greencircle-public-db-scrape.py
  python 2026-08-12-greencircle-public-db-scrape.py --out D:\\gcc-mirror
  python 2026-08-12-greencircle-public-db-scrape.py --metadata-only
  python 2026-08-12-greencircle-public-db-scrape.py --dry-run --limit 25
  python 2026-08-12-greencircle-public-db-scrape.py --company GAF
  python 2026-08-12-greencircle-public-db-scrape.py --certification "Recycled Content"

  # signed-in portal (all statuses). Set the cookie in the SAME shell first:
  #   $env:GCC_PORTAL_COOKIE = '<__Secure-authjs.session-token value>'
  python 2026-08-12-greencircle-public-db-scrape.py --portal --out C:\\GCCPortalPull
  python 2026-08-12-greencircle-public-db-scrape.py --portal --dry-run --limit 25
  python 2026-08-12-greencircle-public-db-scrape.py --portal --status Expired

The portal session cookie (verified 2026-08-13)
-----------------------------------------------
  The portal is tRPC behind NextAuth -- a DIFFERENT protocol from the public REST
  route, so the index call differs; downloads are identical:

      GET /api/trpc/certificate.getAll?batch=1&input=... -> ALL records, no paging
      GET /api/certificates/{id}/download                -> 307 -> presigned S3

  The cookie is read from the environment, never from argv, and never written to
  disk by this script. It expires -- a 401, or a 200 whose envelope holds no
  record array, means re-copy it. requests drops the Cookie header across the
  redirect to S3, so the session never leaves greencirclecertified.com.

Requires: requests  (pip install requests)

Heads-up on size: the PDFs are full-resolution scans and run large (multi-MB
each). Expect the complete mirror to be on the order of a couple of GB -- point
--out somewhere with room, and preferably outside a synced OneDrive folder.
"""

from __future__ import annotations

import argparse
import csv
import datetime
import hashlib
import json
import os
import re
import sys
import time
import urllib.robotparser
from urllib.parse import urljoin, urlparse, unquote

try:
    import requests
except ImportError:
    sys.exit("ERROR: requests is required.  Run:  pip install requests")

# --------------------------------------------------------------------------- #
# Configuration
# --------------------------------------------------------------------------- #

BASE_URL = "https://db.greencirclecertified.com"
INDEX_PATH = "/api/certificates"
DOWNLOAD_PATH = "/api/certificates/{id}/download"

# --- authenticated portal (--portal) ---------------------------------------- #
# The signed-in portal is a DIFFERENT protocol from the public REST route above:
# it is tRPC, behind a NextAuth session cookie. Verified 2026-08-13 by reading the
# site's own JS chunks -- `/api/portal/certificates` does not exist, don't guess.
# `certificate.getAll` takes NO arguments and returns the whole set in one call
# (6,000 records), exactly like the public route. Downloads are UNCHANGED, which is
# why this is a flag on the proven script rather than a second script.
PORTAL_INDEX_PATH = (
    "/api/trpc/certificate.getAll?batch=1&input="
    '{"0":{"json":null,"meta":{"values":["undefined"]}}}'
)
# The session cookie is a live credential: it is read from the environment only --
# never a CLI argument (argv shows up in process lists and shell history).
COOKIE_ENV = "GCC_PORTAL_COOKIE"
# Verified against the live site 2026-08-13: this deployment is Auth.js v5, so the
# cookie is `__Secure-authjs.session-token`. The older `next-auth.*` names are kept
# as fallbacks. Getting this wrong does NOT look like a naming problem -- the server
# just returns 401, which reads as a bad or expired session. If a bare token 401s,
# suspect the name before you suspect the value.
COOKIE_NAMES = (
    "__Secure-authjs.session-token",      # Auth.js v5, https  <- this site
    "authjs.session-token",               # Auth.js v5, http
    "__Secure-next-auth.session-token",   # NextAuth v4, https
    "next-auth.session-token",            # NextAuth v4, http
)

USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
    "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
)

DEFAULT_OUT = "./_gcc-public-mirror"
DEFAULT_DELAY = 1.0
MAX_RETRIES = 4
BACKOFF_BASE = 2.0          # seconds; doubled each attempt
CONNECT_TIMEOUT = 15
READ_TIMEOUT = 120
RETRY_STATUS = {408, 425, 429, 500, 502, 503, 504}

# Columns as they appear on the public page, plus provenance/bookkeeping fields.
CSV_COLUMNS = [
    "certificate_number",
    "company",
    "product",
    "product_type",
    "certification",
    "effective_date",
    "expiration_date",
    "location",
    "contact",
    "tags",
    "cert_status",       # portal only: Expired / Client Approved - Paid / ...
    "document_count",    # portal only: 0 means there is no PDF to fetch
    "document_name",     # portal only: the source system's own file name
    "record_id",
    "download_url",
    "source_file_name",
    "local_path",
    "file_size_bytes",
    "sha256",
    "status",
    "status_detail",
    "http_status",
    "checked_at_utc",
    "downloaded_at_utc",
]

# --------------------------------------------------------------------------- #
# Logging
# --------------------------------------------------------------------------- #


class Log:
    """Console + file logger. Deliberately tiny; no external dependency."""

    def __init__(self, path: str | None = None):
        self.fh = open(path, "a", encoding="utf-8") if path else None

    def __call__(self, msg: str = "", level: str = "INFO") -> None:
        stamp = datetime.datetime.now().strftime("%H:%M:%S")
        line = msg if not msg else f"[{stamp}] {level:<5} {msg}"
        print(line, flush=True)
        if self.fh:
            self.fh.write(line + "\n")
            self.fh.flush()

    def close(self) -> None:
        if self.fh:
            self.fh.close()


# --------------------------------------------------------------------------- #
# Helpers
# --------------------------------------------------------------------------- #

_UNSAFE = re.compile(r'[<>:"/\\|?*\x00-\x1f]')
_COLLAPSE = re.compile(r"[\s_]+")


def sanitize(value: str, max_len: int = 60) -> str:
    """Make an arbitrary field safe for a Windows/POSIX filename component."""
    text = _UNSAFE.sub("-", (value or "").strip())
    text = _COLLAPSE.sub("_", text)
    text = text.strip("._-")
    if len(text) > max_len:
        text = text[:max_len].rstrip("._-")
    return text or "unknown"


def iso_to_date(value: str | None) -> str:
    """'2026-08-01T04:00:00.000Z' -> '2026-08-01'. Blank stays blank."""
    if not value:
        return ""
    return str(value)[:10]


def dig(record: dict, *path: str) -> str:
    """Safely walk nested dicts that may be null at any level."""
    node = record
    for key in path:
        if not isinstance(node, dict):
            return ""
        node = node.get(key)
    return "" if node is None else str(node)


def flatten(record: dict) -> dict:
    """API record -> flat metadata row keyed to the public table's columns."""
    tags = record.get("tags") or []
    tag_names = [t.get("name", str(t)) if isinstance(t, dict) else str(t) for t in tags]
    record_id = record.get("id")
    # Portal records carry two fields the public route never returns. `documents`
    # is the one that matters operationally: 425 of the 6,000 portal records have
    # an empty list, i.e. there is no file to fetch and a request would be wasted.
    #
    # ABSENT and EMPTY must stay distinguishable. The public route omits the key
    # entirely, so treating a missing key as "no documents" makes the skip below
    # fire on every public record -- which is exactly what it did on first run.
    # Absent -> "" (unknown, always attempt); empty list -> 0 (known, skip).
    documents = record.get("documents")
    has_doc_info = isinstance(documents, list)
    documents = documents if has_doc_info else []
    return {
        "certificate_number": record.get("certificateNumber") or "",
        "company": dig(record, "company", "name"),
        "product": dig(record, "product", "name"),
        "product_type": dig(record, "product", "productType", "name"),
        "certification": dig(record, "certificateType", "name"),
        "effective_date": iso_to_date(record.get("effectiveDate")),
        "expiration_date": iso_to_date(record.get("expirationDate")),
        "location": record.get("location") or "",
        "contact": record.get("contact") or "",
        "tags": "; ".join(tag_names),
        "cert_status": dig(record, "status", "status"),
        "document_count": len(documents) if has_doc_info else "",
        "document_name": (documents[0].get("name", "") if documents
                          and isinstance(documents[0], dict) else ""),
        "record_id": record_id,
        "download_url": urljoin(BASE_URL, DOWNLOAD_PATH.format(id=record_id)),
        "source_file_name": "",
        "local_path": "",
        "file_size_bytes": "",
        "sha256": "",
        "status": "",
        "status_detail": "",
        "http_status": "",
        "checked_at_utc": "",
        "downloaded_at_utc": "",
    }


def build_filename(row: dict) -> str:
    """
    <CertNo>_<Company>_<id>.pdf

    The record id is always included: 12 of the 1021 live records share a
    certificate number with another record, so cert number alone collides.
    """
    return "{}_{}_{}.pdf".format(
        sanitize(row["certificate_number"], 40),
        sanitize(row["company"], 50),
        row["record_id"],
    )


def sha256_of(path: str) -> str:
    digest = hashlib.sha256()
    with open(path, "rb") as fh:
        for block in iter(lambda: fh.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def utcnow() -> str:
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


# --------------------------------------------------------------------------- #
# Politeness
# --------------------------------------------------------------------------- #


class Robots:
    """robots.txt gate. A missing/unreadable robots.txt means 'no rules'."""

    def __init__(self, session: requests.Session, log: Log):
        self.parser = None
        self.crawl_delay = None
        url = urljoin(BASE_URL, "/robots.txt")
        try:
            resp = session.get(url, timeout=(CONNECT_TIMEOUT, 30))
            if resp.status_code == 200 and "html" not in resp.headers.get(
                "Content-Type", ""
            ):
                self.parser = urllib.robotparser.RobotFileParser()
                self.parser.parse(resp.text.splitlines())
                self.crawl_delay = self.parser.crawl_delay(USER_AGENT)
                log(f"robots.txt loaded; crawl-delay={self.crawl_delay}")
            else:
                log(f"robots.txt: HTTP {resp.status_code} / no rules published")
        except requests.RequestException as exc:
            log(f"robots.txt could not be fetched ({exc}); proceeding with no rules",
                "WARN")

    def allows(self, url: str) -> bool:
        if self.parser is None:
            return True
        return self.parser.can_fetch(USER_AGENT, url)


def request_with_retry(
    session: requests.Session,
    url: str,
    log: Log,
    stream: bool = False,
) -> requests.Response:
    """GET with exponential backoff on transient failures. Raises on give-up."""
    last_error: Exception | None = None
    for attempt in range(1, MAX_RETRIES + 1):
        try:
            resp = session.get(
                url, stream=stream, timeout=(CONNECT_TIMEOUT, READ_TIMEOUT)
            )
            if resp.status_code in RETRY_STATUS:
                wait = BACKOFF_BASE * (2 ** (attempt - 1))
                retry_after = resp.headers.get("Retry-After")
                if retry_after and retry_after.isdigit():
                    wait = max(wait, int(retry_after))
                resp.close()
                if attempt == MAX_RETRIES:
                    raise requests.HTTPError(
                        f"HTTP {resp.status_code} after {MAX_RETRIES} attempts",
                        response=resp,
                    )
                log(f"  HTTP {resp.status_code}; retry {attempt}/{MAX_RETRIES - 1} "
                    f"in {wait:.0f}s", "WARN")
                time.sleep(wait)
                continue
            return resp
        except (requests.ConnectionError, requests.Timeout) as exc:
            last_error = exc
            if attempt == MAX_RETRIES:
                break
            wait = BACKOFF_BASE * (2 ** (attempt - 1))
            log(f"  {type(exc).__name__}; retry {attempt}/{MAX_RETRIES - 1} "
                f"in {wait:.0f}s", "WARN")
            time.sleep(wait)
    raise requests.ConnectionError(
        f"gave up after {MAX_RETRIES} attempts: {last_error}"
    )


# --------------------------------------------------------------------------- #
# Steps
# --------------------------------------------------------------------------- #


def fetch_index(session: requests.Session, log: Log, out_dir: str,
                portal: bool = False) -> list[dict]:
    """Pull the complete record list and archive the raw response."""
    url = urljoin(BASE_URL, PORTAL_INDEX_PATH if portal else INDEX_PATH)
    log(f"Fetching record index: {url}")
    resp = request_with_retry(session, url, log)
    if portal and resp.status_code in (401, 403):
        raise ValueError(
            f"HTTP {resp.status_code} from the portal -- the session cookie in "
            f"${COOKIE_ENV} is missing, expired, or for a signed-out session. "
            f"Re-copy it from the browser and set it again."
        )
    resp.raise_for_status()
    payload = resp.json()

    if portal:
        # tRPC batch envelope: [{"result":{"data":{"json":[ ...records... ]}}}]
        try:
            data = payload[0]["result"]["data"]
            records = data["json"] if isinstance(data, dict) and "json" in data else data
        except (KeyError, IndexError, TypeError) as exc:
            raise ValueError(f"unexpected tRPC envelope shape: {exc}") from exc
        # A signed-out portal request returns 200 with an error envelope, not a 401.
        if not isinstance(records, list):
            raise ValueError(
                "portal returned no record array -- almost always an expired "
                f"session cookie. Response began: {str(payload)[:200]}"
            )
    else:
        records = payload

    if not isinstance(records, list):
        raise ValueError(f"expected a JSON array, got {type(records).__name__}")

    raw_path = os.path.join(
        out_dir, "portal-certificates-raw.json" if portal
        else "api-certificates-raw.json")
    with open(raw_path, "w", encoding="utf-8") as fh:
        json.dump(records, fh, indent=2, ensure_ascii=False)
    log(f"Total records reported by the API: {len(records)}")
    log(f"Raw response archived: {raw_path}")
    return records


def apply_filters(rows: list[dict], args, log: Log) -> list[dict]:
    """Optional subset selection. Matching is case-insensitive substring."""
    filters = [
        ("company", args.company),
        ("product_type", args.type),
        ("certification", args.certification),
    ]
    for field, needle in filters:
        if not needle:
            continue
        before = len(rows)
        low = needle.lower()
        rows = [r for r in rows if low in (r[field] or "").lower()]
        log(f"Filter {field} ~ '{needle}': {before} -> {len(rows)} records")

    # Status is matched exactly, not by substring: 'Client Approved - Paid' and
    # 'Client Approved - Confidential' share a prefix, and a substring match on
    # the first would silently sweep in the confidential set.
    if getattr(args, "status", None):
        wanted = set(args.status)
        before = len(rows)
        rows = [r for r in rows if r.get("cert_status") in wanted]
        log(f"Filter cert_status in {sorted(wanted)}: {before} -> {len(rows)} records")

    if args.limit:
        rows = rows[: args.limit]
        log(f"Limit applied: first {len(rows)} records")
    return rows


def load_prior_manifest(out_dir: str, log: Log) -> dict:
    """
    Previous run's manifest, keyed by record id.

    Used only to carry provenance forward onto rows we skip this time round --
    without it, a fully-resumed run would emit a manifest whose S3 source names
    and original download timestamps had all gone blank.
    """
    path = os.path.join(out_dir, "manifest.json")
    if not os.path.exists(path):
        return {}
    try:
        with open(path, encoding="utf-8") as fh:
            payload = json.load(fh)
        prior = {r["record_id"]: r for r in payload.get("records", [])}
        log(f"Prior manifest loaded: {len(prior)} records")
        return prior
    except (OSError, ValueError, KeyError) as exc:
        log(f"Prior manifest unreadable ({exc}); continuing without it", "WARN")
        return {}


def download_one(
    session: requests.Session,
    row: dict,
    cert_dir: str,
    log: Log,
    robots: Robots,
    force: bool,
    dry_run: bool,
    prior: dict,
) -> None:
    """Fetch a single certificate, mutating `row` with the outcome."""
    row["checked_at_utc"] = utcnow()
    filename = build_filename(row)
    dest = os.path.join(cert_dir, filename)
    row["local_path"] = os.path.relpath(dest, os.path.dirname(cert_dir))

    # --- portal: no attached document means there is nothing to fetch ------
    # Recorded as a skip with its own reason so the summary separates "the source
    # system has no file" from "we failed to get the file". 425 of 6,000 records.
    if row.get("document_count") == 0:
        row["status"] = "skipped"
        row["status_detail"] = "no document attached in source system"
        row["local_path"] = ""
        log("  skip (no document in source)")
        return

    # --- resume: already have it? -----------------------------------------
    if not force and os.path.exists(dest) and os.path.getsize(dest) > 0:
        row["status"] = "skipped"
        row["status_detail"] = "already downloaded"
        row["file_size_bytes"] = os.path.getsize(dest)
        row["sha256"] = sha256_of(dest)
        was = prior.get(row["record_id"], {})
        row["source_file_name"] = was.get("source_file_name", "")
        row["downloaded_at_utc"] = was.get("downloaded_at_utc", "")
        row["http_status"] = was.get("http_status", "")
        log(f"  skip (on disk)  {filename}")
        return

    if not robots.allows(row["download_url"]):
        row["status"] = "skipped"
        row["status_detail"] = "disallowed by robots.txt"
        row["local_path"] = ""
        log(f"  skip (robots)   {filename}", "WARN")
        return

    if dry_run:
        row["status"] = "skipped"
        row["status_detail"] = "dry run"
        row["local_path"] = ""
        log(f"  would download  {filename}")
        return

    tmp = dest + ".part"
    try:
        resp = request_with_retry(session, row["download_url"], log, stream=True)
    except requests.RequestException as exc:
        row["status"] = "failed"
        row["status_detail"] = str(exc)[:300]
        row["local_path"] = ""
        log(f"  FAILED          {filename}: {exc}", "ERROR")
        return

    with resp:
        row["http_status"] = resp.status_code

        # 401/403/404 here means "not public" rather than "broken" -- the
        # database states plainly that not every awarded certificate is
        # published. Record it and move on; never try to get around it.
        if resp.status_code in (401, 403, 404, 410):
            row["status"] = "skipped"
            row["status_detail"] = f"not publicly downloadable (HTTP {resp.status_code})"
            row["local_path"] = ""
            log(f"  skip (HTTP {resp.status_code}) {filename}")
            return

        if resp.status_code != 200:
            row["status"] = "failed"
            row["status_detail"] = f"unexpected HTTP {resp.status_code}"
            row["local_path"] = ""
            log(f"  FAILED          {filename}: HTTP {resp.status_code}", "ERROR")
            return

        # The 307 lands on a presigned S3 object; keep its real name for audit.
        final_name = os.path.basename(urlparse(resp.url).path)
        row["source_file_name"] = unquote(final_name)

        try:
            with open(tmp, "wb") as fh:
                for chunk in resp.iter_content(chunk_size=256 * 1024):
                    if chunk:
                        fh.write(chunk)
        except (OSError, requests.RequestException) as exc:
            if os.path.exists(tmp):
                os.remove(tmp)
            row["status"] = "failed"
            row["status_detail"] = f"write/transfer error: {exc}"[:300]
            row["local_path"] = ""
            log(f"  FAILED          {filename}: {exc}", "ERROR")
            return

    size = os.path.getsize(tmp)
    if size == 0:
        os.remove(tmp)
        row["status"] = "failed"
        row["status_detail"] = "empty response body"
        row["local_path"] = ""
        log(f"  FAILED          {filename}: empty body", "ERROR")
        return

    with open(tmp, "rb") as fh:
        magic = fh.read(5)
    if magic != b"%PDF-":
        row["status_detail"] = f"warning: not a PDF header ({magic!r})"
        log(f"  note: {filename} does not start with %PDF-", "WARN")

    os.replace(tmp, dest)          # atomic: a killed run never leaves a half file
    row["status"] = "downloaded"
    row["file_size_bytes"] = size
    row["sha256"] = sha256_of(dest)
    row["downloaded_at_utc"] = utcnow()
    log(f"  downloaded      {filename}  ({size / 1024 / 1024:.1f} MB)")


def write_manifests(rows: list[dict], records: list[dict], out_dir: str, log: Log) -> None:
    csv_path = os.path.join(out_dir, "manifest.csv")
    with open(csv_path, "w", encoding="utf-8-sig", newline="") as fh:
        writer = csv.DictWriter(fh, fieldnames=CSV_COLUMNS, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)

    by_id = {r.get("id"): r for r in records}
    json_path = os.path.join(out_dir, "manifest.json")
    payload = {
        "source": urljoin(BASE_URL, "/certificate"),
        "api_endpoint": urljoin(BASE_URL, INDEX_PATH),
        "generated_at_utc": utcnow(),
        "total_records_in_source": len(records),
        "records_in_manifest": len(rows),
        "records": [
            {**row, "api_record": by_id.get(row["record_id"])} for row in rows
        ],
    }
    with open(json_path, "w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2, ensure_ascii=False)

    log(f"Manifest (CSV):  {csv_path}")
    log(f"Manifest (JSON): {json_path}")


# --------------------------------------------------------------------------- #
# Main
# --------------------------------------------------------------------------- #


def main() -> int:
    ap = argparse.ArgumentParser(
        description="Mirror the GreenCircle Certified public certificate database.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    ap.add_argument("--out", default=DEFAULT_OUT,
                    help=f"output directory (default: {DEFAULT_OUT})")
    ap.add_argument("--delay", type=float, default=DEFAULT_DELAY,
                    help=f"seconds between requests (default: {DEFAULT_DELAY})")
    ap.add_argument("--limit", type=int,
                    help="only process the first N records (testing)")
    ap.add_argument("--company", help="only records whose company contains this")
    ap.add_argument("--type", help="only records whose product type contains this")
    ap.add_argument("--certification",
                    help="only records whose certification contains this")
    ap.add_argument("--metadata-only", action="store_true",
                    help="refresh the manifest only; download nothing")
    ap.add_argument("--dry-run", action="store_true",
                    help="report what would be downloaded without fetching files")
    ap.add_argument("--force", action="store_true",
                    help="re-download even if the file is already on disk")
    ap.add_argument("--portal", action="store_true",
                    help=f"pull the SIGNED-IN portal (all statuses, ~6000 records) "
                         f"instead of the public database. Requires a NextAuth "
                         f"session cookie in ${COOKIE_ENV}.")
    ap.add_argument("--status", action="append", metavar="NAME",
                    help="portal only: restrict to this certificate status "
                         "(repeatable, exact match, e.g. --status Expired)")
    args = ap.parse_args()

    cookie = ""
    if args.portal:
        cookie = os.environ.get(COOKIE_ENV, "").strip()
        if not cookie:
            print(
                f"ERROR: --portal needs a session cookie in ${COOKIE_ENV}.\n\n"
                f"  1. Sign in to {BASE_URL}/portal/certificate\n"
                f"  2. F12 -> Application -> Cookies -> copy the value of\n"
                f"     {COOKIE_NAMES[0]} (or {COOKIE_NAMES[1]})\n"
                f"  3. PowerShell, same window you run this from:\n"
                f"     $env:{COOKIE_ENV} = '<paste>'\n\n"
                f"It is read from the environment on purpose -- a cookie passed as\n"
                f"a CLI argument would land in shell history and process lists.",
                file=sys.stderr)
            return 2
        # Accept either a bare token or a full 'name=value; name2=value2' header,
        # so it works whether the value was copied from the cookie table or from
        # DevTools' "Copy as cURL".
        if "=" not in cookie:
            cookie = f"{COOKIE_NAMES[0]}={cookie}"

    out_dir = os.path.abspath(args.out)
    cert_dir = os.path.join(out_dir, "certificates")
    os.makedirs(cert_dir, exist_ok=True)

    # Keep a multi-GB mirror out of git no matter where --out points.
    gitignore = os.path.join(out_dir, ".gitignore")
    if not os.path.exists(gitignore):
        with open(gitignore, "w", encoding="utf-8") as fh:
            fh.write("# Mirrored third-party content -- never commit.\n*\n")

    started = time.time()
    log = Log(os.path.join(
        out_dir, f"scrape-log-{datetime.datetime.now():%Y%m%d-%H%M%S}.txt"))

    log("=" * 78)
    log("GreenCircle Certified " + ("SIGNED-IN PORTAL pull" if args.portal
                                    else "public database mirror"))
    log("=" * 78)
    log(f"Source : {urljoin(BASE_URL, '/portal/certificate' if args.portal else '/certificate')}")
    log(f"Output : {out_dir}")
    log(f"Mode   : " + (
        "metadata only" if args.metadata_only else
        "dry run" if args.dry_run else
        "force re-download" if args.force else "download (resumable)"))
    log(f"Delay  : {args.delay}s between requests")
    log("")

    session = requests.Session()
    session.headers.update({
        "User-Agent": USER_AGENT,
        "Accept": "application/json, application/pdf, */*",
        "Accept-Language": "en-US,en;q=0.9",
    })
    if args.portal:
        session.headers["Cookie"] = cookie
        # requests strips a Cookie header when a redirect crosses hosts, which is
        # exactly what we want: the download 307s to presigned S3 and the session
        # cookie must NOT travel to Amazon.
        log("Auth   : session cookie from $" + COOKIE_ENV)

    robots = Robots(session, log)
    delay = args.delay
    if robots.crawl_delay and robots.crawl_delay > delay:
        delay = float(robots.crawl_delay)
        log(f"Honouring robots.txt Crawl-delay: {delay}s")

    if not robots.allows(urljoin(BASE_URL, INDEX_PATH)):
        log("robots.txt disallows the certificate API. Stopping.", "ERROR")
        log.close()
        return 2

    try:
        records = fetch_index(session, log, out_dir, portal=args.portal)
    except (requests.RequestException, ValueError) as exc:
        log(f"Could not retrieve the record index: {exc}", "ERROR")
        log.close()
        return 1

    rows = [flatten(r) for r in records]
    rows = apply_filters(rows, args, log)
    total = len(rows)
    prior = load_prior_manifest(out_dir, log)
    log("")

    if args.metadata_only:
        for row in rows:
            row["status"] = "skipped"
            row["status_detail"] = "metadata-only run"
            row["checked_at_utc"] = utcnow()
            dest = os.path.join(cert_dir, build_filename(row))
            if os.path.exists(dest) and os.path.getsize(dest) > 0:
                row["status_detail"] = "metadata-only run (file already on disk)"
                row["local_path"] = os.path.relpath(dest, out_dir)
                row["file_size_bytes"] = os.path.getsize(dest)
        log(f"Metadata-only: {total} records catalogued, no files fetched.")
    else:
        log(f"Processing {total} records...")
        for index, row in enumerate(rows, start=1):
            log(f"[{index}/{total}] {row['certificate_number']} - "
                f"{row['company']}")
            download_one(session, row, cert_dir, log, robots,
                         args.force, args.dry_run, prior)
            # Only pause when we actually hit the network.
            if index < total and row["status"] != "skipped":
                time.sleep(delay)

    log("")
    write_manifests(rows, records, out_dir, log)

    # ----------------------------------------------------------------- summary
    downloaded = [r for r in rows if r["status"] == "downloaded"]
    skipped = [r for r in rows if r["status"] == "skipped"]
    failed = [r for r in rows if r["status"] == "failed"]
    have_file = downloaded + [r for r in skipped if r["local_path"]]
    total_bytes = sum(int(r["file_size_bytes"] or 0) for r in have_file)
    elapsed = time.time() - started

    log("")
    log("=" * 78)
    log("SUMMARY")
    log("=" * 78)
    log(f"Records in source database : {len(records)}")
    log(f"Records processed          : {total}")
    if args.portal:
        no_doc = [r for r in rows if r.get("document_count") == 0]
        log(f"  of those, no document    : {len(no_doc)}  "
            f"(source system holds no file -- not a failure)")
    log(f"  downloaded this run      : {len(downloaded)}")
    log(f"  skipped                  : {len(skipped)}")
    log(f"  failed                   : {len(failed)}")
    log(f"Certificates held locally  : {len(have_file)} of {total} "
        f"({total_bytes / 1024 / 1024:.1f} MB)")
    log(f"Elapsed                    : {elapsed / 60:.1f} min")

    reasons: dict[str, int] = {}
    for row in skipped:
        reasons[row["status_detail"]] = reasons.get(row["status_detail"], 0) + 1
    if reasons:
        log("")
        log("Skip reasons:")
        for reason, count in sorted(reasons.items(), key=lambda kv: -kv[1]):
            log(f"  {count:>5}  {reason}")

    if failed:
        log("")
        log(f"FAILURES ({len(failed)}) -- re-run to retry these:")
        for row in failed:
            log(f"  id={row['record_id']}  {row['certificate_number']}  "
                f"{row['company']}  ->  {row['status_detail']}", "ERROR")
    else:
        log("")
        log("No failures.")

    log.close()
    return 1 if failed else 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        print("\nInterrupted. Re-run to resume; completed files are kept.")
        sys.exit(130)
