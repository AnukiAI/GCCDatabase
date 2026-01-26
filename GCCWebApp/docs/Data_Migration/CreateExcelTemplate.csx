// Script to generate Excel template - Run with: dotnet script CreateExcelTemplate.csx
#r "nuget: ClosedXML, 0.102.1"

using ClosedXML.Excel;
using System;
using System.IO;

var workbook = new XLWorkbook();

// ============================================
// TAB 1: Instructions
// ============================================
var instructionsSheet = workbook.Worksheets.Add("Instructions");

int row = 1;
void AddHeader(string text) {
    instructionsSheet.Cell(row, 1).Value = text;
    instructionsSheet.Cell(row, 1).Style.Font.Bold = true;
    instructionsSheet.Cell(row, 1).Style.Font.FontSize = 14;
    row++;
}

void AddSubHeader(string text) {
    instructionsSheet.Cell(row, 1).Value = text;
    instructionsSheet.Cell(row, 1).Style.Font.Bold = true;
    instructionsSheet.Cell(row, 1).Style.Font.FontSize = 12;
    row++;
}

void AddText(string text) {
    instructionsSheet.Cell(row, 1).Value = text;
    row++;
}

void AddBlank() { row++; }

void AddTableRow(string col1, string col2, string col3 = "", string col4 = "") {
    instructionsSheet.Cell(row, 1).Value = col1;
    instructionsSheet.Cell(row, 2).Value = col2;
    if (!string.IsNullOrEmpty(col3)) instructionsSheet.Cell(row, 3).Value = col3;
    if (!string.IsNullOrEmpty(col4)) instructionsSheet.Cell(row, 4).Value = col4;
    row++;
}

AddHeader("GCC Certificate Import Template - Instructions");
AddBlank();
AddText("This template allows you to import all your certificate data into the GCC Certificate Management System.");
AddText("The system will automatically create Companies, Products, and other related data from this file.");
AddBlank();

AddSubHeader("HOW TO USE THIS TEMPLATE");
AddText("1. Go to the 'Certificate Data' tab");
AddText("2. Review the sample data rows to understand the format");
AddText("3. Delete the sample rows and enter your own data");
AddText("4. Save the file");
AddText("5. Send to GCC team for import");
AddBlank();

AddSubHeader("REQUIRED FIELDS");
AddTableRow("Column", "Format", "Description");
instructionsSheet.Range(row-1, 1, row-1, 3).Style.Font.Bold = true;
AddTableRow("CERTIFICATE_NUMBER", "YY-NNNN", "Unique certificate number (e.g., 23-0358, 24-1597)");
AddTableRow("COMPANY_NAME", "Text (255 chars)", "Legal name of the company");
AddTableRow("CERTIFICATION_TYPE", "See list below", "Must match exactly");
AddTableRow("EFFECTIVE_DATE", "M/D/YYYY", "Date certificate becomes effective");
AddBlank();

AddSubHeader("RECOMMENDED FIELDS");
AddTableRow("EXPIRATION_DATE", "M/D/YYYY", "Leave blank for perpetual certificates");
AddTableRow("PRODUCT_NAME", "Text", "Use 'Not Specified' if unknown");
AddTableRow("CERTIFICATE_COUNTRY", "Full name", "IMPORTANT: Enables country filtering");
AddBlank();

AddSubHeader("VALID CERTIFICATION TYPES (use exact spelling)");
AddText("• Recycled Content");
AddText("• GreenCircle Audited");
AddText("• Certified Environmental Facts");
AddText("• Waste Diversion from Landfill");
AddText("• Carbon Footprint Reduction");
AddText("• Material Ingredient Reporting");
AddText("• Certified Sustainability Facts");
AddText("• LCA Optimized");
AddText("• Certified Energy Savings");
AddText("• Closed Loop Product");
AddBlank();

AddSubHeader("VALID COUNTRY VALUES (use full names)");
AddTableRow("Correct", "Avoid");
instructionsSheet.Range(row-1, 1, row-1, 2).Style.Font.Bold = true;
AddTableRow("United States", "USA, US, U.S.");
AddTableRow("Canada", "CA, CAN");
AddTableRow("United Kingdom", "UK, U.K., Britain");
AddTableRow("Mexico", "MX, MEX");
AddBlank();

AddSubHeader("PARTNER VALUES");
AddText("• Amazon - Climate Pledge Friendly program");
AddText("• Novi - Novi Connect program");
AddText("• None - Not associated with a partner (or leave blank)");
AddBlank();

AddSubHeader("COMMON TAGS (comma-separated)");
AddText("APR, LEED, FSC, Energy Star, SCS, USDA BioPreferred, Cradle to Cradle, Green Seal");
AddBlank();

AddSubHeader("IMPORTANT NOTES");
AddText("• Certificate numbers must be unique");
AddText("• Use M/D/YYYY date format (e.g., 6/1/2023)");
AddText("• Country enables filtering - always fill in CERTIFICATE_COUNTRY");
AddText("• Company names must be consistent across all rows");
AddText("• Use 'Not Specified' if product name is unknown");
AddText("• If a value contains commas, Excel will handle quoting automatically");
AddText("• Leave cells blank if data not available (don't enter N/A or None)");

instructionsSheet.Column(1).Width = 50;
instructionsSheet.Column(2).Width = 30;
instructionsSheet.Column(3).Width = 50;

// ============================================
// TAB 2: Certificate Data (the actual template)
// ============================================
var dataSheet = workbook.Worksheets.Add("Certificate Data");

// Headers
string[] headers = {
    "CERTIFICATE_NUMBER", "COMPANY_NAME", "COMPANY_LOCATION", "COMPANY_COUNTRY", "COMPANY_EMAIL", "COMPANY_WEBSITE",
    "PRODUCT_NAME", "PRODUCT_TYPE", "CERTIFICATION_TYPE", "EFFECTIVE_DATE", "EXPIRATION_DATE",
    "CERTIFICATE_LOCATION", "CERTIFICATE_COUNTRY", "PARTNER", "CERTIFICATION_RESULTS", "TAGS",
    "ASIN", "UPC", "GTIN", "EAN"
};

for (int i = 0; i < headers.Length; i++) {
    dataSheet.Cell(1, i + 1).Value = headers[i];
    dataSheet.Cell(1, i + 1).Style.Font.Bold = true;
    dataSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
}

// Sample data
object[][] sampleData = {
    new object[] { "23-0358", "Sofidel America Corp.", "Horsham PA", "United States", "", "", "Plastic Film Packaging", "Packaging", "Recycled Content", "6/1/2023", "", "Horsham PA", "United States", "None", "95% post-consumer recycled content", "APR", "", "", "", "" },
    new object[] { "23-0375", "IB Roof Systems", "", "United States", "", "", "+Vantage Vinyl Verified", "Roofing", "GreenCircle Audited", "6/1/2023", "5/31/2026", "", "United States", "None", "", "", "", "", "", "" },
    new object[] { "23-0753", "DUDE Wipes", "Springdale AR", "United States", "", "", "DUDE Wipes Flushable Wipes", "Wipes", "Certified Environmental Facts", "10/1/2023", "9/30/2026", "Springdale AR", "United States", "None", "", "", "", "", "", "" },
    new object[] { "23-0923", "Cox Automotive", "Riverside CA", "United States", "", "", "Not Specified", "", "Waste Diversion from Landfill", "11/1/2023", "10/31/2026", "Riverside CA", "United States", "None", "92% waste diverted", "", "", "", "", "" },
    new object[] { "23-0925", "Procter & Gamble", "Cincinnati OH", "United States", "", "", "Electric Toothbrush Handles", "Consumer Goods", "Carbon Footprint Reduction", "11/1/2023", "10/31/2026", "Gyongyos, Hungary", "Hungary", "None", "30% carbon reduction", "", "B08XYZ1234", "012345678901", "", "" },
    new object[] { "24-0028", "MAPEI", "Deerfield Beach FL", "United States", "", "", "Ker 111 Gray & White", "Building Materials", "Material Ingredient Reporting", "3/1/2024", "2/28/2026", "Deerfield Beach FL", "United States", "None", "", "", "", "", "", "" },
    new object[] { "25-0245", "HID Global", "Cardiff", "United Kingdom", "", "", "HID Signo readers", "Electronics", "Certified Energy Savings", "7/1/2025", "6/30/2026", "Cardiff, Wales", "United Kingdom", "Amazon", "Energy Star certified", "Energy Star", "B0DEF12345", "", "", "" },
    new object[] { "25-8484", "Clover Imaging Group", "Ottawa IL", "United States", "", "", "Remanufactured Ink Cartridges", "Consumer Products", "Closed Loop Product", "12/1/2025", "11/30/2026", "Ottawa IL", "United States", "Amazon", "", "", "", "", "", "" },
};

for (int r = 0; r < sampleData.Length; r++) {
    for (int c = 0; c < sampleData[r].Length; c++) {
        dataSheet.Cell(r + 2, c + 1).Value = sampleData[r][c]?.ToString() ?? "";
    }
}

// Format columns
dataSheet.Columns().AdjustToContents();

// ============================================
// TAB 3: Valid Values Reference
// ============================================
var refSheet = workbook.Worksheets.Add("Valid Values");

refSheet.Cell(1, 1).Value = "CERTIFICATION TYPES";
refSheet.Cell(1, 1).Style.Font.Bold = true;
string[] certTypes = { "Recycled Content", "GreenCircle Audited", "Certified Environmental Facts",
    "Waste Diversion from Landfill", "Carbon Footprint Reduction", "Material Ingredient Reporting",
    "Certified Sustainability Facts", "LCA Optimized", "Certified Energy Savings", "Closed Loop Product" };
for (int i = 0; i < certTypes.Length; i++) {
    refSheet.Cell(i + 2, 1).Value = certTypes[i];
}

refSheet.Cell(1, 3).Value = "COUNTRIES";
refSheet.Cell(1, 3).Style.Font.Bold = true;
string[] countries = { "United States", "Canada", "United Kingdom", "Mexico", "Germany", "Hungary",
    "Indonesia", "El Salvador", "China", "Japan", "Australia", "France", "Italy", "Spain", "Brazil" };
for (int i = 0; i < countries.Length; i++) {
    refSheet.Cell(i + 2, 3).Value = countries[i];
}

refSheet.Cell(1, 5).Value = "PARTNERS";
refSheet.Cell(1, 5).Style.Font.Bold = true;
refSheet.Cell(2, 5).Value = "Amazon";
refSheet.Cell(3, 5).Value = "Novi";
refSheet.Cell(4, 5).Value = "None";

refSheet.Cell(1, 7).Value = "TAGS";
refSheet.Cell(1, 7).Style.Font.Bold = true;
string[] tags = { "APR", "LEED", "FSC", "Energy Star", "SCS", "USDA BioPreferred", "Cradle to Cradle", "Green Seal" };
for (int i = 0; i < tags.Length; i++) {
    refSheet.Cell(i + 2, 7).Value = tags[i];
}

refSheet.Cell(1, 9).Value = "PRODUCT TYPES";
refSheet.Cell(1, 9).Style.Font.Bold = true;
string[] productTypes = { "Packaging", "Building Materials", "Consumer Goods", "Consumer Products",
    "Electronics", "Food", "Textiles", "Plastic", "Plastic Film", "Plastic Resins", "Wipes",
    "Roofing", "Asphalt", "Door" };
for (int i = 0; i < productTypes.Length; i++) {
    refSheet.Cell(i + 2, 9).Value = productTypes[i];
}

refSheet.Columns().AdjustToContents();

// Save the file
string outputPath = Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory) ?? ".",
    "docs", "Data_Migration", "templates", "GCC_Certificate_Import_Template.xlsx");

// Try current directory approach
outputPath = "GCC_Certificate_Import_Template.xlsx";
workbook.SaveAs(outputPath);

Console.WriteLine($"Excel file created: {outputPath}");
