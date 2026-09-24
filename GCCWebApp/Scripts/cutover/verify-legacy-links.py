"""Verify every legacy public certificate link lands on the right, displaying certificate.

Input: legacy-public-certificates.json (the old site's /api/certificates feed, 983 records).
For each record, both old link forms are requested WITHOUT following redirects:
  /certificate/{id}                 and   /api/certificates/{id}/download
Expected: 301 -> /Certificate/CertificateSingleView/{that record's certificateNumber}.
Then each distinct target is fetched once: expected 200 + "Certificate Details" title.
Output: legacy-link-results.csv (one row per link) + summary on stdout.
"""
import csv, json, os, sys, time, urllib.parse
import http.client, ssl
from concurrent.futures import ThreadPoolExecutor

HERE = os.path.dirname(os.path.abspath(__file__))
HOST = "db.greencirclecertified.com"
CTX = ssl.create_default_context()
# PIN the NEW server. Local DNS cannot be trusted here: a hosts-file entry (added to view the
# old site) sent run 1 to the legacy Vercel host - caught only because the canaries failed.
NEW_SERVER_IP = "64.27.52.66"


class PinnedHTTPS(http.client.HTTPSConnection):
    def connect(self):
        import socket
        sock = socket.create_connection((NEW_SERVER_IP, 443), self.timeout)
        self.sock = CTX.wrap_socket(sock, server_hostname=HOST)  # SNI + cert check against the real name


def req(path, tries=5):
    for attempt in range(tries):
        try:
            c = PinnedHTTPS(HOST, timeout=40, context=CTX)
            c.request("GET", path, headers={"User-Agent": "gcc-legacy-link-verify"})
            r = c.getresponse()
            body = r.read()
            loc = r.getheader("Location") or ""
            c.close()
            if r.status == 429 or r.status >= 500:
                time.sleep(2 * (attempt + 1)); continue
            return r.status, loc, body
        except Exception as e:  # network error: retry, then record it honestly
            last = repr(e); time.sleep(2 * (attempt + 1))
    return -1, "", b""  # -1 = no response after retries (never a fabricated status)


recs = json.load(open(os.path.join(HERE, "legacy-public-certificates.json"), encoding="utf-8"))
# CANARIES - the instrument must flag these, or a clean run proves nothing:
#   real id with a deliberately wrong expected number -> must report WRONG CERT
#   id that never existed                             -> must report NOT REDIRECTED
CANARIES = [{"id": 5815, "certificateNumber": "99-0000", "expirationDate": "CANARY"},
            {"id": 99999999, "certificateNumber": "99-0001", "expirationDate": "CANARY"}]
links = []
for r in recs + CANARIES:
    for kind, path in (("preview", f"/certificate/{r['id']}"),
                       ("download", f"/api/certificates/{r['id']}/download")):
        links.append({"old_id": r["id"], "expected_number": r.get("certificateNumber", ""),
                      "expiration": str(r.get("expirationDate", ""))[:10], "kind": kind, "old_link": path})


def check_link(l):
    st, loc, _ = req(l["old_link"])
    got = ""
    if loc:
        tail = urllib.parse.urlparse(loc).path
        if "/Certificate/CertificateSingleView/" in tail:
            got = urllib.parse.unquote(tail.rsplit("/", 1)[-1])
    l.update(redirect_status=st, redirect_to=loc, landed_number=got,
             number_matches=(got == l["expected_number"]))
    return l


with ThreadPoolExecutor(4) as ex:
    links = list(ex.map(check_link, links))

targets = sorted({l["landed_number"] for l in links if l["landed_number"]})


def check_target(n):
    st, _, body = req("/Certificate/CertificateSingleView/" + urllib.parse.quote(n))
    shows = st == 200 and b"Certificate Details" in body and n.encode() in body
    return n, st, shows, body[:120].decode("utf-8", "replace") if not shows else ""


with ThreadPoolExecutor(4) as ex:
    tres = {n: (st, shows, snip) for n, st, shows, snip in ex.map(check_target, targets)}

for l in links:
    st, shows, snip = tres.get(l["landed_number"], ("", False, ""))
    l.update(target_status=st, target_displays=shows, target_message=snip)
    l["outcome"] = ("OK" if l["redirect_status"] == 301 and l["number_matches"] and shows
                    else "WRONG CERT" if l["landed_number"] and not l["number_matches"]
                    else "NOT REDIRECTED" if not l["landed_number"]
                    else "LANDS BUT NOT SHOWN")

out = os.path.join(HERE, "legacy-link-results.csv")
with open(out, "w", newline="", encoding="utf-8") as f:
    w = csv.DictWriter(f, fieldnames=list(links[0].keys())); w.writeheader(); w.writerows(links)

from collections import Counter
print("CANARIES (must NOT be OK):")
for l in links:
    if l["expiration"] == "CANARY":
        print("  ", l["kind"], l["old_link"], "->", l["outcome"])
links = [l for l in links if l["expiration"] != "CANARY"]
print("records:", len(recs), " links tested:", len(links), " distinct targets:", len(targets))
for (k, o), n in sorted(Counter((l["kind"], l["outcome"]) for l in links).items()):
    print(f"  {k:9} {o:22} {n}")
print("redirect statuses:", dict(Counter(l["redirect_status"] for l in links)))
bad = [l for l in links if l["outcome"] != "OK"]
print("non-OK rows:", len(bad))
for l in bad[:40]:
    print("  ", l["kind"], l["old_link"], "expected", l["expected_number"], "->", l["redirect_status"],
          l["landed_number"] or l["redirect_to"][:60], "| target", l["target_status"], l["target_message"][:70])
print("csv:", out)
