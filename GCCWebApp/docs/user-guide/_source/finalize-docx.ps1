# Fills in the table of contents in built guide .docx files, using Word itself.
#
# WHY THIS EXISTS: build-guide.py writes the contents as a Word TOC *field*. A field is only a
# placeholder until Word computes it, so every .docx opened on the client's machine showed page 2
# as "Right-click and choose Update Field to build the contents." The HTML never had the problem,
# which is why it went unnoticed until the guides were first opened in Word (2026-09-25).
# Only Word can paginate, so only Word can fill it in: this opens each file, updates every TOC,
# and saves in place.
#
# Run AFTER build-guide.py, on a machine with Word installed:
#   powershell -File finalize-docx.ps1                 # every GCC_*.docx in docs/user-guide
#   powershell -File finalize-docx.ps1 GCC_Company_User_Guide_v1.0.docx
param([string[]]$Files)

$dir = Split-Path -Parent $PSScriptRoot   # docs/user-guide
if (-not $Files) { $Files = Get-ChildItem $dir -Filter 'GCC_*.docx' | ForEach-Object Name }

$w = New-Object -ComObject Word.Application
$w.Visible = $false
$w.DisplayAlerts = 0
$failed = 0
try {
    foreach ($f in $Files) {
        $path = Join-Path $dir $f
        $d = $w.Documents.Open($path, $false, $false)
        foreach ($toc in $d.TablesOfContents) { $toc.Update() }
        $d.Repaginate()
        $text = $d.Range().Text
        if ($d.TablesOfContents.Count -eq 0 -or $text -match 'Update Field to build the contents') {
            Write-Output "  FAILED  $f  (contents still a placeholder)"
            $failed++
        } else {
            $entries = $d.TablesOfContents.Item(1).Range.Paragraphs.Count
            Write-Output ("  ok      {0}  ({1} contents lines, {2} pages)" -f $f, $entries, $d.ComputeStatistics(2))
        }
        $d.Save()
        $d.Close()
    }
} finally {
    $w.Quit()
}
if ($failed) { exit 1 }
