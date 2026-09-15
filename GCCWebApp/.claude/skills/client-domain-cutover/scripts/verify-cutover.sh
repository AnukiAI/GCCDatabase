#!/usr/bin/env bash
# verify-cutover.sh — external verification of a hostname cutover.
#
# Usage:
#   verify-cutover.sh -n NEW_HOST [-o OLD_HOST] [-p PLACEHOLDER_HOST] [-i EXPECTED_IP] [-d DEEPLINKS_FILE]
#
#   -n  new client hostname that should serve the app (required)
#   -o  old hostname that should 301 to NEW_HOST (optional)
#   -p  placeholder/dev hostname that should 301 to NEW_HOST (optional)
#   -i  IP the new hostname should resolve to (optional; skips check if omitted)
#   -d  file of paths (one per line, e.g. /certificate?id=123) to test on OLD_HOST
#
# Run from a machine OUTSIDE the hosting network. Exit code is non-zero if any
# check fails. Requires: dig, curl, openssl.

set -u
NEW=""; OLD=""; PH=""; IP=""; DEEP=""
while getopts "n:o:p:i:d:" opt; do
  case $opt in
    n) NEW=$OPTARG ;; o) OLD=$OPTARG ;; p) PH=$OPTARG ;; i) IP=$OPTARG ;; d) DEEP=$OPTARG ;;
    *) echo "usage: $0 -n NEW_HOST [-o OLD_HOST] [-p PLACEHOLDER] [-i IP] [-d deeplinks.txt]"; exit 2 ;;
  esac
done
[ -z "$NEW" ] && { echo "-n NEW_HOST is required"; exit 2; }

FAIL=0
pass() { printf "  PASS  %s\n" "$1"; }
fail() { printf "  FAIL  %s\n" "$1"; FAIL=1; }

hdr() { curl -sS -o /dev/null -I -m 15 "$1" -w '%{http_code} %{redirect_url}\n' 2>/dev/null; }

check_dns() {
  local h=$1
  echo "DNS: $h"
  local a; a=$(dig +short "$h" | tail -n1)
  local c; c=$(dig +short CNAME "$h")
  [ -n "$c" ] && echo "        CNAME -> $c"
  if [ -z "$a" ]; then fail "$h does not resolve"; return; fi
  echo "        resolves to $a"
  if [ -n "$IP" ] && [ "$h" = "$NEW" ]; then
    [ "$a" = "$IP" ] && pass "resolves to expected IP" || fail "expected $IP got $a"
  fi
}

check_tls() {
  local h=$1
  echo "TLS: $h"
  local out; out=$(echo | openssl s_client -servername "$h" -connect "$h:443" 2>/dev/null | openssl x509 -noout -subject -enddate -ext subjectAltName 2>/dev/null)
  if [ -z "$out" ]; then fail "no certificate presented"; return; fi
  echo "$out" | sed 's/^/        /'
  echo "$out" | grep -q "$h" && pass "cert covers $h" || fail "cert does not list $h"
}

check_serves() {
  local h=$1
  echo "SERVE: https://$h/"
  local r; r=$(hdr "https://$h/")
  local code=${r%% *}
  [ "$code" = "200" ] && pass "200 with no redirect" || fail "got '$r' (expected 200, no redirect)"
  r=$(hdr "http://$h/")
  code=${r%% *}; loc=${r#* }
  if [ "$code" = "301" ] || [ "$code" = "308" ]; then
    case "$loc" in https://$h/*) pass "http -> https ($code)";; *) fail "http redirect goes to $loc";; esac
  else fail "http://$h/ returned $code (expected 301 to https)"; fi
  local hsts; hsts=$(curl -sSI -m 15 "https://$h/" | grep -i '^strict-transport-security' | tr -d '\r')
  [ -n "$hsts" ] && pass "HSTS present" || echo "  WARN  no HSTS header"
}

check_redirect() {
  local from=$1 path=${2:-/}
  local r; r=$(hdr "https://$from$path")
  local code=${r%% *}; local loc=${r#* }
  local want="https://$NEW$path"
  if [ "$code" = "301" ] || [ "$code" = "308" ]; then
    [ "$loc" = "$want" ] && pass "$from$path -> $loc ($code)" || fail "$from$path -> $loc (expected $want)"
  else
    fail "$from$path returned $code (expected 301)"
  fi
}

check_chain() {
  local from=$1 path=${2:-/}
  local hops; hops=$(curl -sS -o /dev/null -L -m 20 "https://$from$path" -w '%{num_redirects}')
  [ "${hops:-9}" -le 1 ] && pass "redirect chain length $hops" || fail "redirect chain length $hops (>1 hop)"
}

echo "=== NEW HOST ==="
check_dns "$NEW"; check_tls "$NEW"; check_serves "$NEW"

if [ -n "$OLD" ]; then
  echo; echo "=== OLD HOST (must 301 to $NEW, path+query preserved) ==="
  check_dns "$OLD"; check_tls "$OLD"
  check_redirect "$OLD" "/"; check_chain "$OLD" "/"
  check_redirect "$OLD" "/certificate?id=TEST123&x=1"
  if [ -n "$DEEP" ] && [ -f "$DEEP" ]; then
    while IFS= read -r p; do [ -n "$p" ] && check_redirect "$OLD" "$p"; done < "$DEEP"
  fi
fi

if [ -n "$PH" ]; then
  echo; echo "=== PLACEHOLDER HOST (must 301 to $NEW) ==="
  check_dns "$PH"; check_redirect "$PH" "/"; check_redirect "$PH" "/some/path?q=1"
fi

echo
[ $FAIL -eq 0 ] && echo "ALL CHECKS PASSED" || echo "ONE OR MORE CHECKS FAILED"
exit $FAIL
