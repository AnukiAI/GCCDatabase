# GCC Database - PDF Export Guide

This directory contains professional HTML versions of migration documentation that can be easily converted to PDF for client distribution.

## Available Documents

1. **Data_Migration_Strategy.html** - Comprehensive migration strategy (850+ lines, 24KB)
2. **Field_Mapping_Reference.html** - Field-by-field mapping guide (coming soon)

## How to Convert to PDF

### Option 1: Browser Print (Recommended - Easiest)

1. **Open HTML file in browser:**
   - Double-click `Data_Migration_Strategy.html`
   - Or right-click → Open with → Chrome/Edge/Firefox

2. **Print to PDF:**
   - Click the "🖨️ Print to PDF" button (top-right corner)
   - Or press `Ctrl+P` (Windows) / `Cmd+P` (Mac)
   - Select "Save as PDF" as the destination
   - Click "Save"

3. **PDF Settings (Recommended):**
   - Paper size: Letter (8.5 x 11 inches)
   - Margins: Default
   - Scale: 100%
   - Options: Enable "Background graphics" for colors

### Option 2: Microsoft Edge (Best Quality)

1. Open file in Microsoft Edge browser
2. Press `Ctrl+P` to open Print dialog
3. Select "Microsoft Print to PDF"
4. Click "More settings" and configure:
   - Layout: Portrait
   - Paper size: Letter
   - Margins: Default
   - Scale: 100%
   - Options: ✓ Background graphics, ✓ Headers and footers
5. Click "Print" and choose save location

### Option 3: Google Chrome (Cross-Platform)

1. Open file in Google Chrome
2. Press `Ctrl+P` (Windows) or `Cmd+P` (Mac)
3. Destination: "Save as PDF"
4. Click "More settings":
   - Paper size: Letter
   - Margins: Default
   - Scale: Default
   - Options: ✓ Background graphics
5. Click "Save"

### Option 4: Command Line (Advanced - Requires wkhtmltopdf)

**Install wkhtmltopdf:**
- Windows: Download from https://wkhtmltopdf.org/downloads.html
- Mac: `brew install wkhtmltopdf`
- Linux: `sudo apt-get install wkhtmltopdf`

**Convert to PDF:**
```bash
# Data Migration Strategy
wkhtmltopdf --page-size Letter --margin-top 0.75in --margin-bottom 0.75in --margin-left 0.75in --margin-right 0.75in Data_Migration_Strategy.html Data_Migration_Strategy.pdf

# Field Mapping Reference
wkhtmltopdf --page-size Letter --margin-top 0.75in --margin-bottom 0.75in --margin-left 0.75in --margin-right 0.75in Field_Mapping_Reference.html Field_Mapping_Reference.pdf
```

### Option 5: PowerShell Script (Windows Only)

Run the provided PowerShell script:

```powershell
.\Convert-To-PDF.ps1
```

This will automatically convert all HTML files in this directory to PDF using Microsoft Edge.

## PDF Output Quality

The HTML documents are optimized for PDF export with:

- **Professional styling** - Clean typography, GCC brand colors (#002B76 blue, #92C83E green)
- **Print-optimized layout** - Proper page breaks, margins, headers
- **Tables** - Formatted with borders, alternating row colors, header styling
- **Code blocks** - Monospace font, background color, left border accent
- **Callout boxes** - Info (blue), Warning (yellow), Success (green), Critical (red)
- **Table of Contents** - Clickable links (in PDF) for easy navigation
- **Page numbering** - Automatic via browser print
- **Headers/Footers** - Document title, version, page numbers

## Customization

To customize the PDF output:

1. **Modify HTML file** - Edit the `<style>` section in the HTML file
2. **Colors** - Change `#002B76` (dark blue) and `#92C83E` (green) to your brand colors
3. **Fonts** - Change `font-family: 'Segoe UI'` to your preferred font
4. **Margins** - Adjust `@page { margin: 0.75in; }` for different margins
5. **Page size** - Change `@page { size: letter; }` to `A4` for international clients

## Troubleshooting

### Issue: PDF has no colors/backgrounds

**Solution:** Enable "Background graphics" in print settings

### Issue: Page breaks in wrong places

**Solution:** Add `page-break-after: avoid;` to specific elements in CSS

### Issue: Tables split across pages awkwardly

**Solution:** Tables already have `page-break-inside: avoid;` - ensure print scale is 100%

### Issue: Fonts look different in PDF

**Solution:** Use web-safe fonts (Segoe UI, Arial, Times New Roman) or embed custom fonts

### Issue: PDF file size too large

**Solution:**
- Reduce image quality in print settings
- Remove "Background graphics" option
- Use Chrome instead of Edge (smaller file sizes)

## File Sizes (Approximate)

- **Data_Migration_Strategy.pdf** - 350-500 KB (depending on browser)
- **Field_Mapping_Reference.pdf** - 400-600 KB (depending on browser)

## Alternative: Markdown to PDF

If you prefer to work with the original Markdown files, you can convert them directly:

### Using Pandoc (Requires Installation)

```bash
# Install pandoc
# Windows: choco install pandoc
# Mac: brew install pandoc
# Linux: sudo apt-get install pandoc

# Convert Markdown to PDF
pandoc ../DATA_MIGRATION_STRATEGY.md -o Data_Migration_Strategy.pdf --pdf-engine=wkhtmltopdf
pandoc ../FIELD_MAPPING_REFERENCE.md -o Field_Mapping_Reference.pdf --pdf-engine=wkhtmltopdf
```

### Using Visual Studio Code Extension

1. Install "Markdown PDF" extension in VS Code
2. Open markdown file
3. Right-click → "Markdown PDF: Export (pdf)"

## Support

For questions or issues with PDF conversion:
- **Email:** swest@anukiconsulting.com
- **Alternative formats:** Request DOCX, XLSX, or other formats if needed

---

**Last Updated:** November 21, 2025
**Version:** 1.0
