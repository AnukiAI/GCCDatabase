#!/bin/bash
# Upload Amazon test data to GCC site
# Usage: bash upload_to_remote.sh [site_url]
# Example: bash upload_to_remote.sh https://gccwebapp.com
#          bash upload_to_remote.sh http://localhost:5101

SITE="${1:-https://gccwebapp.com}"
# Use the script's directory for finding PDF files
# On Git Bash/Windows, convert Unix paths back to Windows paths for curl
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CSV_DIR="$(cygpath -w "$SCRIPT_DIR" 2>/dev/null || echo "$SCRIPT_DIR")"
COOKIE_FILE="/tmp/gcc_remote_cookies.txt"

echo "=== GCC Amazon Test Data Upload Script ==="
echo "Target: $SITE"
echo ""

# Step 1: Login
echo "[1/4] Logging in as admin..."
curl -s -c "$COOKIE_FILE" "$SITE/Account/Login" > /tmp/gcc_login.html
TOKEN=$(cat /tmp/gcc_login.html | grep -o 'value="CfDJ8[^"]*"' | head -1 | sed 's/value="//;s/"$//')

if [ -z "$TOKEN" ]; then
    echo "ERROR: Could not get login page token. Is the site up?"
    exit 1
fi

curl -s -c "$COOKIE_FILE" -b "$COOKIE_FILE" -L -X POST "$SITE/Account/Login" \
  -d "Email=admin&Password=TestPass123!&__RequestVerificationToken=$TOKEN" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -o /dev/null

# Verify login succeeded
CHECK=$(curl -s -b "$COOKIE_FILE" "$SITE/CertificateImport" -o /dev/null -w "%{http_code}")
if [ "$CHECK" != "200" ]; then
    echo "ERROR: Login failed (got $CHECK). Check credentials."
    exit 1
fi
echo "  Login successful."

# Step 2: Upload CSV (strip local PDF paths - server can't access them)
echo ""
echo "[2/4] Uploading CSV for preview..."

CSV_REMOTE="/tmp/gcc_amazon_import_remote.csv"
head -1 "$CSV_DIR/amazon_test_import.csv" > "$CSV_REMOTE"
tail -n +2 "$CSV_DIR/amazon_test_import.csv" | sed 's/,C:\\[^,]*$/,/' >> "$CSV_REMOTE"

PREVIEW_RESP=$(curl -s -b "$COOKIE_FILE" -X POST "$SITE/api/CertificateImport/Preview" \
  -F "csvFile=@$CSV_REMOTE")

# Parse JSON with python (more reliable than grep for multi-line JSON)
PARSED=$(echo "$PREVIEW_RESP" | python -c "
import json, sys
try:
    d = json.load(sys.stdin)
    print(f\"IMPORT_ID={d['importId']}\")
    print(f\"NEW={d['newCertificates']}\")
    print(f\"MODIFIED={d['modifiedCertificates']}\")
    print(f\"UNCHANGED={d['unchangedCertificates']}\")
    print(f\"ERRORS={d['invalidRows']}\")
    print(f\"CAN_APPLY={d['canApply']}\")
except Exception as e:
    print(f'PARSE_ERROR={e}')
" 2>&1)

eval "$PARSED"

echo "  Import ID: $IMPORT_ID"
echo "  New: $NEW | Modified: $MODIFIED | Unchanged: $UNCHANGED | Errors: $ERRORS"

if [ "$CAN_APPLY" != "True" ]; then
    echo "ERROR: Import cannot be applied (canApply=$CAN_APPLY)."
    echo "$PREVIEW_RESP" | python -m json.tool 2>/dev/null || echo "$PREVIEW_RESP"
    exit 1
fi

# Step 3: Apply the import
if [ "$NEW" = "0" ] && [ "$MODIFIED" = "0" ]; then
    echo "  No new or modified certificates. Skipping apply."
else
    echo ""
    echo "[3/4] Applying import..."
    APPLY_RESP=$(curl -s -b "$COOKIE_FILE" -X POST "$SITE/api/CertificateImport/Apply" \
      -H "Content-Type: application/json" \
      -d "{\"importId\":\"$IMPORT_ID\",\"reason\":\"Amazon test data load\"}")

    APPLY_PARSED=$(echo "$APPLY_RESP" | python -c "
import json, sys
try:
    d = json.load(sys.stdin)
    print(f\"SUCCESS={d['success']}\")
    print(f\"CREATED={d['certificatesCreated']}\")
    print(f\"UPDATED={d['certificatesUpdated']}\")
    print(f\"FAILED={d['certificatesFailed']}\")
    if d.get('errors'):
        for e in d['errors']:
            print(f'  Error: {e}')
except Exception as e:
    print(f'PARSE_ERROR={e}')
" 2>&1)

    eval "$(echo "$APPLY_PARSED" | grep -E '^(SUCCESS|CREATED|UPDATED|FAILED)=')"

    if [ "$SUCCESS" != "True" ]; then
        echo "ERROR: Import failed."
        echo "$APPLY_PARSED"
        exit 1
    fi
    echo "  Created: $CREATED | Updated: $UPDATED | Failed: $FAILED"
fi

# Step 4: Upload PDFs via the dedicated API endpoint
echo ""
echo "[4/4] Uploading PDFs..."

PDF_COUNT=0
PDF_OK=0
for PDF_FILE in "$CSV_DIR"/26-*.pdf; do
    CERT_NUM=$(basename "$PDF_FILE" .pdf)
    PDF_COUNT=$((PDF_COUNT + 1))

    RESP=$(curl -s -b "$COOKIE_FILE" -X POST "$SITE/api/certificates/$CERT_NUM/upload-pdf" \
      -F "pdfFile=@$PDF_FILE;type=application/pdf")

    IS_SUCCESS=$(echo "$RESP" | python -c "
import json, sys
try:
    d = json.load(sys.stdin)
    print(d.get('success', False))
except:
    print('False')
" 2>&1)

    if [ "$IS_SUCCESS" = "True" ]; then
        echo "  OK   $CERT_NUM"
        PDF_OK=$((PDF_OK + 1))
    else
        MSG=$(echo "$RESP" | python -c "
import json, sys
try:
    d = json.load(sys.stdin)
    print(d.get('message', 'Unknown error'))
except:
    print(sys.stdin.read()[:200])
" 2>&1)
        echo "  FAIL $CERT_NUM: $MSG"
    fi
done

echo ""
echo "=== Complete ==="
echo "  Certificates: $NEW new, $MODIFIED modified, $UNCHANGED unchanged"
echo "  PDFs: $PDF_OK/$PDF_COUNT uploaded successfully"

# Cleanup
rm -f "$COOKIE_FILE" /tmp/gcc_login.html "$CSV_REMOTE"
