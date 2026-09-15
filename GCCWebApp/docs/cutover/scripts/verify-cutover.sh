#!/usr/bin/env bash
# Verify a client-domain cutover from OUTSIDE the network.
#
#   ./verify-cutover.sh <new-host> <expected-ip> [old-host]
#
# Example (GCC, 2026-09-14):
#   ./verify-cutover.sh database.greencirclecertified.com 64.27.52.66 gccwebapp.com
#
# Exits non-zero if any REQUIRED check fails, so it can gate a runbook step.
# Read-only: issues GETs and a HEAD, never a write.

set -uo pipefail

NEW_HOST="${1:?usage: verify-cutover.sh <new-host> <expected-ip> [old-host]}"
EXPECT_IP="${2:?expected IP required}"
OLD_HOST="${3:-}"

PASS=0; FAIL=0; WARN=0
ok()   { printf '  \033[32mPASS\033[0m  %s\n' "$1"; PASS=$((PASS+1)); }
bad()  { printf '  \033[31mFAIL\033[0m  %s\n' "$1"; FAIL=$((FAIL+1)); }
warn() { printf '  \033[33mWARN\033[0m  %s\n' "$1"; WARN=$((WARN+1)); }
head_() { printf '\n\033[1m%s\033[0m\n' "$1"; }

# ---------------------------------------------------------------- DNS
# Git Bash on Windows ships neither dig nor host, so fall back to nslookup.
# Without this the script reports "does not resolve" on Shawn's machine for a
# hostname that resolves perfectly — a false FAIL is worse than no check.
resolve_a() {
  local h="$1"
  if command -v dig >/dev/null 2>&1; then
    dig +short "$h" A | grep -Eo '^[0-9]+(\.[0-9]+){3}$' | tail -n1
  elif command -v host >/dev/null 2>&1; then
    host -t A "$h" 2>/dev/null | awk '/has address/{print $NF}' | tail -n1
  elif command -v nslookup >/dev/null 2>&1; then
    # Skip the resolver's own address block, then take the last answer.
    nslookup "$h" 2>/dev/null \
      | sed -n '/[Nn]ame:/,$p' \
      | awk '/^Address(es)?:/{print $NF} /^[[:space:]]+[0-9]+(\.[0-9]+){3}$/{print $1}' \
      | grep -Eo '^[0-9]+(\.[0-9]+){3}$' | tail -n1
  fi
}

head_ "1. DNS"
if ! command -v dig >/dev/null 2>&1 && ! command -v host >/dev/null 2>&1 \
   && ! command -v nslookup >/dev/null 2>&1; then
  warn "no dig/host/nslookup available — DNS not checked"
else
  GOT_IP=$(resolve_a "$NEW_HOST")
  if [ -z "${GOT_IP:-}" ]; then
    bad "$NEW_HOST does not resolve"
  elif [ "$GOT_IP" = "$EXPECT_IP" ]; then
    ok "$NEW_HOST -> $GOT_IP"
  else
    bad "$NEW_HOST -> $GOT_IP (expected $EXPECT_IP) — not propagated, or wrong record"
  fi

  if command -v dig >/dev/null 2>&1; then
    TTL=$(dig "$NEW_HOST" A +noall +answer | awk '{print $2; exit}')
    [ -n "${TTL:-}" ] && printf '        TTL %ss\n' "$TTL"
  fi
fi

# ---------------------------------------------------------------- TLS
head_ "2. TLS"
CERT=$(echo | openssl s_client -servername "$NEW_HOST" -connect "$NEW_HOST:443" 2>/dev/null \
       | openssl x509 -noout -subject -enddate 2>/dev/null)
if [ -z "$CERT" ]; then
  bad "no TLS handshake with $NEW_HOST"
else
  echo "$CERT" | sed 's/^/        /'
  # Validate the chain AND the hostname match, which is the part that actually breaks.
  if echo | openssl s_client -servername "$NEW_HOST" -connect "$NEW_HOST:443" \
       -verify_return_error -verify_hostname "$NEW_HOST" >/dev/null 2>&1; then
    ok "certificate is valid for $NEW_HOST"
  else
    bad "certificate does NOT validate for $NEW_HOST (name mismatch or bad chain)"
  fi
fi

# ---------------------------------------------------------------- app identity
head_ "3. Application responds on the new host"
SERVER=$(curl -sS -o /dev/null -D- --max-time 25 "https://$NEW_HOST/" 2>/dev/null \
         | awk -F': ' 'tolower($1)=="server"{print $2}' | tr -d '\r')
[ -n "${SERVER:-}" ] && printf '        Server: %s\n' "$SERVER"

# The single most useful check: the partner API must reject an anonymous caller
# with 401. A 200 means we are being served someone else's site; a 3xx means a
# redirect shim is still in the path.
CODE=$(curl -sS -o /dev/null -w '%{http_code}' --max-time 25 \
       "https://$NEW_HOST/api/v1/certificates" 2>/dev/null)
case "$CODE" in
  401) ok  "/api/v1/certificates returns 401 unauthenticated — this is the GCC app" ;;
  200) bad "/api/v1/certificates returns 200 unauthenticated — wrong site, or auth not enforced" ;;
  3*)  bad "/api/v1/certificates returns $CODE — a redirect is still in front of this hostname" ;;
  000) bad "/api/v1/certificates unreachable (timeout / connection refused)" ;;
  *)   bad "/api/v1/certificates returns $CODE — expected 401" ;;
esac

# Redirect shims answer every path identically; the real app does not.
C404=$(curl -sS -o /dev/null -w '%{http_code}' --max-time 25 \
       "https://$NEW_HOST/this-path-should-not-exist-$$" 2>/dev/null)
if [ "$C404" = "$CODE" ] && [ -n "$CODE" ]; then
  warn "a nonsense path returns the same code ($C404) as the API — check for a catch-all redirect"
else
  ok "nonsense path returns $C404 (distinct from the API) — not a catch-all"
fi

# ---------------------------------------------------------------- old host
if [ -n "$OLD_HOST" ]; then
  head_ "4. Previous host ($OLD_HOST)"
  OCODE=$(curl -sS -o /dev/null -w '%{http_code}' --max-time 25 "https://$OLD_HOST/api/v1/certificates" 2>/dev/null)
  OLOC=$(curl -sS -o /dev/null -D- --max-time 25 "https://$OLD_HOST/" 2>/dev/null \
         | awk -F': ' 'tolower($1)=="location"{print $2}' | tr -d '\r')
  printf '        /api/v1/certificates -> %s\n' "$OCODE"
  if [ -n "${OLOC:-}" ]; then
    printf '        / redirects to %s\n' "$OLOC"
    # Not every redirect is a canonical redirect. The pre-launch access gate
    # (project_test_env_access_gate) bounces to /__gate?returnUrl=… and is NOT
    # the cutover redirect — treating it as one produced a false FAIL on
    # 2026-09-14. Identify it and skip.
    case "$OLOC" in
      *__gate*|*/Account/Login*)
        ok "$OLD_HOST redirect is the access gate, not a canonical redirect (expected before step 9)"
        ;;
      *)
        PLOC=$(curl -sS -o /dev/null -D- --max-time 25 "https://$OLD_HOST/Certificate/CertificateTileView?x=1" 2>/dev/null \
               | awk -F': ' 'tolower($1)=="location"{print $2}' | tr -d '\r')
        if [ -n "${PLOC:-}" ]; then
          # The path may be percent-encoded (e.g. carried inside returnUrl), so
          # compare against a decoded copy rather than the raw header.
          DEC=$(printf '%b' "${PLOC//%/\\x}" 2>/dev/null || echo "$PLOC")
          case "$DEC" in
            *Certificate/CertificateTileView*x=1*) ok "old-host redirect preserves path and query" ;;
            *) bad "old-host redirect DROPS path/query: $PLOC" ;;
          esac
        else
          warn "old host redirects / but not a deep path — check the rule covers all paths"
        fi
        ;;
    esac
  else
    ok "$OLD_HOST still serves directly (no canonical redirect yet — expected before step 9)"
  fi
fi

# ---------------------------------------------------------------- result
head_ "Result"
printf '  %d passed, %d failed, %d warnings\n\n' "$PASS" "$FAIL" "$WARN"
[ "$FAIL" -eq 0 ] || exit 1
