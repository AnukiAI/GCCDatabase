# GCC Certificate Import Template - Instructions

## Overview

This template allows you to import all your certificate data into the GCC Certificate Management System in a single file. The system will automatically create Companies, Products, and other related data from this file.

---

## How to Use This Template

1. Open `GCC_Certificate_Import_Template.csv` in Excel or Google Sheets
2. Review the sample data rows to understand the format
3. Delete the sample rows and enter your own data
4. Save as CSV (UTF-8 encoding recommended)
5. Send to GCC team for import

---

## Column Reference

### CERTIFICATE INFORMATION (Required)

| Column | Required | Format | Description |
|--------|----------|--------|-------------|
| **CERTIFICATE_NUMBER** | ✅ YES | `YY-NNNN` | Unique certificate number. Format: 2-digit year, dash, 4-digit sequence. Examples: `23-0358`, `24-1597`, `25-0001` |
| **CERTIFICATION_TYPE** | ✅ YES | Text | The type of certification issued. Must be one of the valid certification types (see list below) |
| **EFFECTIVE_DATE** | ✅ YES | `M/D/YYYY` | Date the certificate becomes effective. Examples: `6/1/2023`, `12/15/2024` |
| **EXPIRATION_DATE** | ⚠️ Recommended | `M/D/YYYY` | Date the certificate expires. Leave blank for perpetual/non-expiring certificates |

### COMPANY INFORMATION

| Column | Required | Max Length | Description |
|--------|----------|------------|-------------|
| **COMPANY_NAME** | ✅ YES | 255 chars | Legal name of the company holding the certificate. Examples: `Sofidel America Corp.`, `General Mills Inc.` |
| **COMPANY_LOCATION** | No | 175 chars | Company headquarters city/state. Examples: `Horsham PA`, `Minneapolis MN` |
| **COMPANY_COUNTRY** | ⚠️ Recommended | 100 chars | Country where company is located. Use full country names (see Country Values below) |
| **COMPANY_EMAIL** | No | 255 chars | Contact email for the company |
| **COMPANY_WEBSITE** | No | 500 chars | Company website URL. Include `https://` |

### PRODUCT INFORMATION

| Column | Required | Max Length | Description |
|--------|----------|------------|-------------|
| **PRODUCT_NAME** | ⚠️ Recommended | 255 chars | Name of the certified product. If unknown, enter `Not Specified` |
| **PRODUCT_TYPE** | No | 255 chars | Category/type of product. Examples: `Packaging`, `Building Materials`, `Consumer Goods`, `Food` |

### CERTIFICATE DETAILS

| Column | Required | Max Length | Description |
|--------|----------|------------|-------------|
| **CERTIFICATE_LOCATION** | No | 175 chars | Manufacturing facility or location where certification applies. Can be different from company location. Examples: `Horsham PA`, `Gyongyos, Hungary and Marktheidenfeld, Germany` |
| **CERTIFICATE_COUNTRY** | ⚠️ Recommended | 100 chars | **IMPORTANT FOR FILTERING** - Country where the certified product is manufactured. This appears in the Country filter on the public site |
| **PARTNER** | No | 100 chars | Partner program association. Values: `Amazon`, `Novi`, `None`, or leave blank |
| **CERTIFICATION_RESULTS** | No | 1000 chars | Optional test results, scores, or metrics. Examples: `95% recycled content`, `30% carbon reduction achieved` |
| **TAGS** | No | - | Comma-separated tags for filtering. Examples: `APR`, `LEED`, `Energy Star`, `APR, LEED` |

### PRODUCT IDENTIFIERS (For Amazon/E-commerce)

| Column | Required | Format | Description |
|--------|----------|--------|-------------|
| **ASIN** | No | 10 chars | Amazon Standard Identification Number. Starts with `B0`. Example: `B08XYZ1234` |
| **UPC** | No | 12 digits | Universal Product Code (barcode). Example: `012345678901` |
| **GTIN** | No | 14 digits | Global Trade Item Number. Example: `00012345678905` |
| **EAN** | No | 13 digits | European Article Number. Example: `5012345678900` |

---

## Valid Certification Types

Use one of these exact values in the CERTIFICATION_TYPE column:

| Certification Type | Description |
|--------------------|-------------|
| `Recycled Content` | Products containing recycled materials |
| `GreenCircle Audited` | Third-party audited environmental claims |
| `Certified Environmental Facts` | Environmental impact certification |
| `Waste Diversion from Landfill` | Waste reduction/diversion certification |
| `Carbon Footprint Reduction` | Carbon emissions reduction certification |
| `Material Ingredient Reporting` | Material composition transparency |
| `Certified Sustainability Facts` | Comprehensive sustainability certification |
| `LCA Optimized` | Life Cycle Assessment optimized products |
| `Certified Energy Savings` | Energy efficiency certification |
| `Closed Loop Product` | Closed loop/circular economy products |

**Note:** If you have a certification type not listed above, please contact the GCC team to add it before import.

---

## Valid Country Values

Use consistent country names for proper filtering:

| Correct Value | Avoid Using |
|---------------|-------------|
| `United States` | USA, US, U.S., America |
| `Canada` | CA, CAN |
| `United Kingdom` | UK, U.K., Britain, England |
| `Mexico` | MX, MEX |
| `Germany` | DE |
| `Hungary` | HU |
| `Indonesia` | ID |
| `El Salvador` | SV |

---

## Valid Partner Values

| Value | Description |
|-------|-------------|
| `Amazon` | Certificate is part of Amazon Climate Pledge Friendly program |
| `Novi` | Certificate is part of Novi Connect program |
| `None` | Not associated with a partner program |
| *(blank)* | Same as None |

---

## Common Tags

These tags appear in the "Additional" filter on the public site:

| Tag | Description |
|-----|-------------|
| `APR` | Association of Plastic Recyclers |
| `LEED` | Leadership in Energy and Environmental Design |
| `FSC` | Forest Stewardship Council |
| `Energy Star` | EPA Energy Star Program |
| `SCS` | SCS Global Services |
| `USDA BioPreferred` | USDA BioPreferred Program |
| `Cradle to Cradle` | Cradle to Cradle Certified |
| `Green Seal` | Green Seal Certification |

To add multiple tags, separate with commas: `APR, LEED, Energy Star`

---

## Examples

### Example 1: Basic Certificate
```
CERTIFICATE_NUMBER: 23-0358
COMPANY_NAME: Sofidel America Corp.
PRODUCT_NAME: Plastic Film Packaging
CERTIFICATION_TYPE: Recycled Content
EFFECTIVE_DATE: 6/1/2023
EXPIRATION_DATE: (leave blank for perpetual)
CERTIFICATE_COUNTRY: United States
```

### Example 2: Certificate with Amazon Integration
```
CERTIFICATE_NUMBER: 25-0245
COMPANY_NAME: HID Global
PRODUCT_NAME: HID Signo readers
CERTIFICATION_TYPE: Certified Energy Savings
EFFECTIVE_DATE: 7/1/2025
EXPIRATION_DATE: 6/30/2026
CERTIFICATE_COUNTRY: United Kingdom
PARTNER: Amazon
ASIN: B0DEF12345
TAGS: Energy Star
```

### Example 3: Certificate with No Product Specified
```
CERTIFICATE_NUMBER: 23-0923
COMPANY_NAME: Cox Automotive
PRODUCT_NAME: Not Specified
CERTIFICATION_TYPE: Waste Diversion from Landfill
EFFECTIVE_DATE: 11/1/2023
EXPIRATION_DATE: 10/31/2026
CERTIFICATE_COUNTRY: United States
CERTIFICATION_RESULTS: 92% waste diverted from landfill
```

---

## Important Notes

1. **Certificate Numbers Must Be Unique** - Each CERTIFICATE_NUMBER can only appear once

2. **Dates** - Use `M/D/YYYY` format (e.g., `6/1/2023` not `06/01/2023` or `2023-06-01`)

3. **Country is Important** - CERTIFICATE_COUNTRY populates the Country filter on the public site. Always fill this in for proper filtering

4. **Company Names Must Be Consistent** - If the same company has multiple certificates, use the exact same spelling for COMPANY_NAME each time

5. **Product Names** - Use `Not Specified` if the product name is unknown or not applicable

6. **Special Characters** - If a value contains commas, wrap the entire value in double quotes: `"Buffalo NY; Cedar Rapids IA"`

7. **Blank Values** - Leave cells blank if data is not available. Do not enter "N/A", "None", or "-"

---

## Checklist Before Submitting

- [ ] All required fields are filled in (CERTIFICATE_NUMBER, COMPANY_NAME, CERTIFICATION_TYPE, EFFECTIVE_DATE)
- [ ] Certificate numbers are in `YY-NNNN` format
- [ ] Dates are in `M/D/YYYY` format
- [ ] Certification types match the valid values list exactly
- [ ] Country names are spelled out fully (United States, not USA)
- [ ] Company names are consistent across all rows for the same company
- [ ] File is saved as CSV format

---

## Need Help?

Contact the GCC team if you have:
- A certification type not in the list
- Questions about how to format your data
- Large volumes of data (1000+ certificates)
- Data in a different format that needs conversion

---

*Template Version: 1.0*
*Last Updated: January 2026*
