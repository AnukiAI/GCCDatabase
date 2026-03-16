using ClosedXML.Excel;

var workbook = new XLWorkbook();

// ============================================
// TAB 1: Instructions
// ============================================
var instructionsSheet = workbook.Worksheets.Add("Instructions");

int row = 1;
void AddHeader(string text)
{
    instructionsSheet.Cell(row, 1).Value = text;
    instructionsSheet.Cell(row, 1).Style.Font.Bold = true;
    instructionsSheet.Cell(row, 1).Style.Font.FontSize = 16;
    instructionsSheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.DarkGreen;
    instructionsSheet.Cell(row, 1).Style.Font.FontColor = XLColor.White;
    instructionsSheet.Range(row, 1, row, 4).Merge();
    row++;
}

void AddSubHeader(string text)
{
    row++;
    instructionsSheet.Cell(row, 1).Value = text;
    instructionsSheet.Cell(row, 1).Style.Font.Bold = true;
    instructionsSheet.Cell(row, 1).Style.Font.FontSize = 12;
    instructionsSheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
    instructionsSheet.Range(row, 1, row, 4).Merge();
    row++;
}

void AddText(string text)
{
    instructionsSheet.Cell(row, 1).Value = text;
    instructionsSheet.Range(row, 1, row, 4).Merge();
    row++;
}

void AddTableHeader(params string[] cols)
{
    for (int i = 0; i < cols.Length; i++)
    {
        instructionsSheet.Cell(row, i + 1).Value = cols[i];
        instructionsSheet.Cell(row, i + 1).Style.Font.Bold = true;
        instructionsSheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
    }
    row++;
}

void AddTableRow(params string[] cols)
{
    for (int i = 0; i < cols.Length; i++)
    {
        instructionsSheet.Cell(row, i + 1).Value = cols[i];
    }
    row++;
}

AddHeader("GCC Certificate Import Template - Instructions");
row++;
AddText("This template allows you to import all your certificate data into the GCC Certificate Management System.");
AddText("The system will automatically create Companies, Products, and other related data from this file.");

AddSubHeader("HOW TO USE THIS TEMPLATE");
AddText("1. Go to the 'Certificate Data' tab");
AddText("2. Review the sample data rows (highlighted in yellow) to understand the format");
AddText("3. Delete the sample rows and enter your own data");
AddText("4. Save the file and send to GCC team for import");

AddSubHeader("REQUIRED FIELDS (must be filled in for every certificate)");
AddTableHeader("Column", "Format", "Example", "Description");
AddTableRow("CERTIFICATE_NUMBER", "YY-NNNN", "23-0358", "Unique certificate number");
AddTableRow("COMPANY_NAME", "Text", "Sofidel America Corp.", "Legal name of the company");
AddTableRow("CERTIFICATION_TYPE", "From list", "Recycled Content", "Must match exactly (see Valid Values tab)");
AddTableRow("EFFECTIVE_DATE", "M/D/YYYY", "6/1/2023", "Date certificate becomes effective");

AddSubHeader("RECOMMENDED FIELDS (important for proper display and filtering)");
AddTableHeader("Column", "Format", "Example", "Description");
AddTableRow("EXPIRATION_DATE", "M/D/YYYY", "5/31/2026", "Leave blank for perpetual certificates");
AddTableRow("PRODUCT_NAME", "Text", "Plastic Film Packaging", "Use 'Not Specified' if unknown");
AddTableRow("CERTIFICATE_COUNTRY", "Full name", "United States", "ENABLES COUNTRY FILTER on public site");
AddTableRow("COMPANY_COUNTRY", "Full name", "United States", "Country where company is located");

AddSubHeader("OPTIONAL FIELDS");
AddTableHeader("Column", "Format", "Example", "Description");
AddTableRow("COMPANY_LOCATION", "Text", "Horsham PA", "Company headquarters city/state");
AddTableRow("COMPANY_EMAIL", "Email", "info@company.com", "Contact email");
AddTableRow("COMPANY_WEBSITE", "URL", "https://www.company.com", "Must include https://");
AddTableRow("PRODUCT_TYPE", "Text", "Packaging", "Category of product");
AddTableRow("CERTIFICATE_LOCATION", "Text", "Manufacturing facility location", "Where product is made");
AddTableRow("PARTNER", "Amazon/Novi/None", "Amazon", "Partner program association");
AddTableRow("CERTIFICATION_RESULTS", "Text", "95% recycled content", "Test results or scores");
AddTableRow("TAGS", "Comma-separated", "APR, LEED", "Tags for filtering");
AddTableRow("ASIN", "10 chars", "B08XYZ1234", "Amazon product ID");
AddTableRow("UPC", "12 digits", "012345678901", "Universal Product Code");
AddTableRow("GTIN", "14 digits", "00012345678905", "Global Trade Item Number");
AddTableRow("EAN", "13 digits", "5012345678900", "European Article Number");

AddSubHeader("IMPORTANT NOTES");
AddText("• Certificate numbers must be UNIQUE - no duplicates allowed");
AddText("• Use M/D/YYYY date format (6/1/2023 not 06/01/2023 or 2023-06-01)");
AddText("• CERTIFICATE_COUNTRY enables the Country filter - always fill this in!");
AddText("• Company names must be CONSISTENT - spell the same company the same way every time");
AddText("• Use 'Not Specified' for PRODUCT_NAME if the product name is unknown");
AddText("• Leave cells BLANK if data not available - don't enter N/A, None, or dashes");
AddText("• See the 'Valid Values' tab for lists of valid certification types, countries, etc.");

instructionsSheet.Column(1).Width = 25;
instructionsSheet.Column(2).Width = 20;
instructionsSheet.Column(3).Width = 25;
instructionsSheet.Column(4).Width = 45;

// ============================================
// TAB 2: Certificate Data (the actual template)
// ============================================
var dataSheet = workbook.Worksheets.Add("Certificate Data");

// Headers with color coding
string[] headers = {
    "CERTIFICATE_NUMBER", "COMPANY_NAME", "COMPANY_LOCATION", "COMPANY_COUNTRY", "COMPANY_EMAIL", "COMPANY_WEBSITE",
    "PRODUCT_NAME", "PRODUCT_TYPE", "CERTIFICATION_TYPE", "EFFECTIVE_DATE", "EXPIRATION_DATE",
    "CERTIFICATE_LOCATION", "CERTIFICATE_COUNTRY", "PARTNER", "CERTIFICATION_RESULTS", "TAGS",
    "ASIN", "UPC", "GTIN", "EAN"
};

// Required columns (0-indexed): 0, 1, 8, 9
int[] requiredCols = { 0, 1, 8, 9 };
// Recommended columns: 6, 10, 12
int[] recommendedCols = { 6, 10, 12 };

for (int i = 0; i < headers.Length; i++)
{
    dataSheet.Cell(1, i + 1).Value = headers[i];
    dataSheet.Cell(1, i + 1).Style.Font.Bold = true;

    if (requiredCols.Contains(i))
    {
        dataSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.Red;
        dataSheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
    }
    else if (recommendedCols.Contains(i))
    {
        dataSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.Orange;
        dataSheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
    }
    else
    {
        dataSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
    }
}

// Add legend row
dataSheet.Cell(2, 1).Value = "RED = Required";
dataSheet.Cell(2, 1).Style.Font.Bold = true;
dataSheet.Cell(2, 1).Style.Font.FontColor = XLColor.Red;
dataSheet.Cell(2, 3).Value = "ORANGE = Recommended";
dataSheet.Cell(2, 3).Style.Font.Bold = true;
dataSheet.Cell(2, 3).Style.Font.FontColor = XLColor.Orange;
dataSheet.Cell(2, 6).Value = "BLUE = Optional";
dataSheet.Cell(2, 6).Style.Font.Bold = true;
dataSheet.Cell(2, 6).Style.Font.FontColor = XLColor.Blue;
dataSheet.Cell(2, 9).Value = "YELLOW ROWS = Sample data (delete before entering your data)";
dataSheet.Cell(2, 9).Style.Font.Bold = true;
dataSheet.Cell(2, 9).Style.Font.FontColor = XLColor.DarkGoldenrod;

// Sample data
object?[][] sampleData = {
    new object?[] { "23-0358", "Sofidel America Corp.", "Horsham PA", "United States", "", "", "Plastic Film Packaging", "Packaging", "Recycled Content", "6/1/2023", "", "Horsham PA", "United States", "None", "95% post-consumer recycled content", "APR", "", "", "", "" },
    new object?[] { "23-0375", "IB Roof Systems", "", "United States", "", "", "+Vantage Vinyl Verified", "Roofing", "GreenCircle Audited", "6/1/2023", "5/31/2026", "", "United States", "None", "", "", "", "", "", "" },
    new object?[] { "23-0753", "DUDE Wipes", "Springdale AR", "United States", "", "", "DUDE Wipes Flushable Wipes", "Wipes", "Certified Environmental Facts", "10/1/2023", "9/30/2026", "Springdale AR", "United States", "None", "", "", "", "", "", "" },
    new object?[] { "23-0923", "Cox Automotive", "Riverside CA", "United States", "", "", "Not Specified", "", "Waste Diversion from Landfill", "11/1/2023", "10/31/2026", "Riverside CA", "United States", "None", "92% waste diverted", "", "", "", "", "" },
    new object?[] { "23-0925", "Procter & Gamble", "Cincinnati OH", "United States", "", "", "Electric Toothbrush Handles", "Consumer Goods", "Carbon Footprint Reduction", "11/1/2023", "10/31/2026", "Gyongyos, Hungary", "Hungary", "None", "30% carbon reduction", "", "B08XYZ1234", "012345678901", "", "" },
    new object?[] { "24-0028", "MAPEI", "Deerfield Beach FL", "United States", "", "", "Ker 111 Gray & White", "Building Materials", "Material Ingredient Reporting", "3/1/2024", "2/28/2026", "Deerfield Beach FL", "United States", "None", "", "", "", "", "", "" },
    new object?[] { "24-0304", "General Mills Inc.", "Minneapolis MN", "United States", "", "", "Various Lucky Charms Cereals", "Food", "Certified Environmental Facts", "4/1/2024", "3/31/2027", "Buffalo NY; Cedar Rapids IA", "United States", "None", "", "", "", "", "", "" },
    new object?[] { "25-0123", "Everrank Inc.", "Tijuana", "Mexico", "", "", "PET Clear PCR Thermoform FDA Resin", "Plastic Resins", "Recycled Content", "2/1/2025", "1/31/2026", "Tijuana", "Mexico", "None", "", "", "", "", "", "" },
    new object?[] { "25-0245", "HID Global", "Cardiff", "United Kingdom", "", "", "HID Signo readers", "Electronics", "Certified Energy Savings", "7/1/2025", "6/30/2026", "Cardiff, Wales", "United Kingdom", "Amazon", "Energy Star certified", "Energy Star", "B0DEF12345", "", "", "" },
    new object?[] { "25-8484", "Clover Imaging Group", "Ottawa IL", "United States", "", "", "Remanufactured Ink Cartridges", "Consumer Products", "Closed Loop Product", "12/1/2025", "11/30/2026", "Ottawa IL", "United States", "Amazon", "", "", "", "", "", "" },
};

for (int r = 0; r < sampleData.Length; r++)
{
    for (int c = 0; c < sampleData[r].Length; c++)
    {
        dataSheet.Cell(r + 3, c + 1).Value = sampleData[r][c]?.ToString() ?? "";
        dataSheet.Cell(r + 3, c + 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
    }
}

// Freeze header row
dataSheet.SheetView.FreezeRows(1);

// Auto-fit columns
dataSheet.Columns().AdjustToContents();

// ============================================
// TAB 3: Valid Values Reference
// ============================================
var refSheet = workbook.Worksheets.Add("Valid Values");

void AddRefSection(int col, string title, string[] values)
{
    refSheet.Cell(1, col).Value = title;
    refSheet.Cell(1, col).Style.Font.Bold = true;
    refSheet.Cell(1, col).Style.Fill.BackgroundColor = XLColor.DarkGreen;
    refSheet.Cell(1, col).Style.Font.FontColor = XLColor.White;

    for (int i = 0; i < values.Length; i++)
    {
        refSheet.Cell(i + 2, col).Value = values[i];
    }
}

string[] certTypes = {
    "Recycled Content",
    "GreenCircle Audited",
    "Certified Environmental Facts",
    "Waste Diversion from Landfill",
    "Carbon Footprint Reduction",
    "Material Ingredient Reporting",
    "Certified Sustainability Facts",
    "LCA Optimized",
    "Certified Energy Savings",
    "Closed Loop Product"
};

string[] countries = {
    "United States",
    "Canada",
    "United Kingdom",
    "Mexico",
    "Germany",
    "Hungary",
    "Indonesia",
    "El Salvador",
    "China",
    "Japan",
    "Australia",
    "France",
    "Italy",
    "Spain",
    "Brazil",
    "India"
};

string[] partners = { "Amazon", "Novi", "None" };

string[] tags = {
    "APR",
    "LEED",
    "FSC",
    "Energy Star",
    "SCS",
    "USDA BioPreferred",
    "Cradle to Cradle",
    "Green Seal"
};

string[] productTypes = {
    "Packaging",
    "Building Materials",
    "Consumer Goods",
    "Consumer Products",
    "Electronics",
    "Food",
    "Textiles",
    "Plastic",
    "Plastic Film",
    "Plastic Resins",
    "Wipes",
    "Roofing",
    "Asphalt",
    "Door",
    "Card Reader"
};

AddRefSection(1, "CERTIFICATION TYPES (use exactly as shown)", certTypes);
AddRefSection(3, "COUNTRIES (use full names)", countries);
AddRefSection(5, "PARTNERS", partners);
AddRefSection(7, "TAGS (comma-separate multiple)", tags);
AddRefSection(9, "PRODUCT TYPES (examples)", productTypes);

refSheet.Columns().AdjustToContents();

// Save the file
string outputPath = Path.Combine(args.Length > 0 ? args[0] : ".", "GCC_Certificate_Import_Template.xlsx");
workbook.SaveAs(outputPath);

Console.WriteLine($"Excel file created successfully: {outputPath}");
Console.WriteLine();
Console.WriteLine("Template includes:");
Console.WriteLine("  - Instructions tab with detailed field descriptions");
Console.WriteLine("  - Certificate Data tab with color-coded headers and sample data");
Console.WriteLine("  - Valid Values tab with dropdown reference lists");
