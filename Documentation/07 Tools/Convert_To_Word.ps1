# PowerShell script to convert Markdown to Word document
$markdownFile = "GCC_Test_Strategy_and_Plan.md"
$wordFile = "GCC_Test_Strategy_and_Plan.docx"

# Create Word application object
$word = New-Object -ComObject Word.Application
$word.Visible = $false

# Create new document
$doc = $word.Documents.Add()
$selection = $word.Selection

# Read markdown content
$content = Get-Content $markdownFile -Raw

# Split content into sections
$sections = $content -split "(?=^#{1,3} )"

foreach ($section in $sections) {
    if ($section -match "^# (.+)") {
        # Main heading
        $selection.Style = "Heading 1"
        $selection.TypeText($matches[1])
        $selection.TypeParagraph()
    }
    elseif ($section -match "^## (.+)") {
        # Sub heading
        $selection.Style = "Heading 2"
        $selection.TypeText($matches[1])
        $selection.TypeParagraph()
    }
    elseif ($section -match "^### (.+)") {
        # Sub-sub heading
        $selection.Style = "Heading 3"
        $selection.TypeText($matches[1])
        $selection.TypeParagraph()
    }
    
    # Process the rest of the section
    $lines = $section -split "`n" | Select-Object -Skip 1
    foreach ($line in $lines) {
        if ($line -match "^#### (.+)") {
            $selection.Style = "Heading 4"
            $selection.TypeText($matches[1])
            $selection.TypeParagraph()
        }
        elseif ($line -match "^##### (.+)") {
            $selection.Style = "Heading 5"
            $selection.TypeText($matches[1])
            $selection.TypeParagraph()
        }
        elseif ($line -match "^\|") {
            # Table row - simplified handling
            $selection.Style = "Normal"
            $selection.TypeText($line)
            $selection.TypeParagraph()
        }
        elseif ($line -match "^[\d]+\. (.+)") {
            # Numbered list
            $selection.Style = "List Number"
            $selection.TypeText($matches[1])
            $selection.TypeParagraph()
        }
        elseif ($line -match "^- (.+)") {
            # Bullet list
            $selection.Style = "List Bullet"
            $selection.TypeText($matches[1])
            $selection.TypeParagraph()
        }
        elseif ($line -match "^\*\*(.+?)\*\*") {
            # Bold text
            $selection.Style = "Normal"
            $selection.Font.Bold = $true
            $selection.TypeText($line -replace "\*\*", "")
            $selection.Font.Bold = $false
            $selection.TypeParagraph()
        }
        elseif ($line.Trim() -ne "" -and $line -notmatch "^``" -and $line -notmatch "^---") {
            # Normal paragraph
            $selection.Style = "Normal"
            $selection.TypeText($line)
            $selection.TypeParagraph()
        }
    }
}

# Save as Word document
$doc.SaveAs([System.IO.Path]::GetFullPath($wordFile), 16) # 16 = wdFormatDocumentDefault
$doc.Close()
$word.Quit()

Write-Host "Word document created successfully: $wordFile"