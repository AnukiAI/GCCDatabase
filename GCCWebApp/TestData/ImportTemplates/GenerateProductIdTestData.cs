using System;
using System.IO;
using System.Text;

namespace GCCWebApp.TestData;

/// <summary>
/// Generates CSV test files for Product ID bulk import testing
/// Run with: dotnet run --project GCCWebApp -- generate-productid-testdata
/// Or compile and run directly
/// </summary>
public static class GenerateProductIdTestData
{
    private static readonly Random _random = new(42); // Fixed seed for reproducible data
    private static readonly string[] ProductIdTypes = { "ASIN", "UPC", "GTIN", "EAN", "ISBN", "SKU", "MPN" };
    private static readonly string[] Partners = { "Amazon", "Novi", "None", "Other" };
    private static readonly string[] Statuses = { "Active", "Active", "Active", "Inactive", "Pending" };

    public static void Main(string[] args)
    {
        var outputDir = args.Length > 0 ? args[0] : ".";

        Console.WriteLine("Generating Product ID test data files...");

        // Generate different sized files
        GenerateFile(Path.Combine(outputDir, "productid_small.csv"), 100);
        GenerateFile(Path.Combine(outputDir, "productid_medium.csv"), 1000);
        GenerateFile(Path.Combine(outputDir, "productid_large.csv"), 5000);
        GenerateFile(Path.Combine(outputDir, "productid_massive.csv"), 10000);
        GenerateFile(Path.Combine(outputDir, "productid_stress.csv"), 50000);

        // Generate validation test files
        GenerateInvalidFormatFile(Path.Combine(outputDir, "productid_invalid_formats.csv"));
        GenerateDuplicatesFile(Path.Combine(outputDir, "productid_duplicates.csv"));
        GenerateMixedValidityFile(Path.Combine(outputDir, "productid_mixed_validity.csv"));

        Console.WriteLine("Done! Test data files generated.");
    }

    private static void GenerateFile(string path, int count)
    {
        Console.WriteLine($"Generating {path} with {count} records...");

        var sb = new StringBuilder();
        sb.AppendLine("Type,Value,Status,Partner,ProductName,ProductSku,Description");

        for (int i = 0; i < count; i++)
        {
            var type = ProductIdTypes[_random.Next(ProductIdTypes.Length)];
            var value = GenerateValidProductId(type, i);
            var status = Statuses[_random.Next(Statuses.Length)];
            var partner = Partners[_random.Next(Partners.Length)];
            var productName = $"Test Product {i + 1} ({type})";
            var sku = $"SKU-{i + 1:D8}";
            var description = i % 3 == 0 ? $"Test description for product {i + 1}" : "";

            sb.AppendLine($"{type},{value},{status},{partner},\"{productName}\",{sku},\"{description}\"");
        }

        File.WriteAllText(path, sb.ToString());
        Console.WriteLine($"  Created: {path} ({new FileInfo(path).Length / 1024} KB)");
    }

    private static void GenerateInvalidFormatFile(string path)
    {
        Console.WriteLine($"Generating {path} with invalid format records...");

        var sb = new StringBuilder();
        sb.AppendLine("Type,Value,Status,Partner,ProductName,ProductSku,Description");

        // Invalid ASINs
        sb.AppendLine("ASIN,TOOLONG1234,Active,Amazon,Invalid ASIN too long,SKU001,");
        sb.AppendLine("ASIN,SHORT,Active,Amazon,Invalid ASIN too short,SKU002,");
        sb.AppendLine("ASIN,B0@#$%^&*!,Active,Amazon,Invalid ASIN special chars,SKU003,");

        // Invalid UPCs
        sb.AppendLine("UPC,12345,Active,None,Invalid UPC too short,SKU004,");
        sb.AppendLine("UPC,1234567890123,Active,None,Invalid UPC too long,SKU005,");
        sb.AppendLine("UPC,12345678901A,Active,None,Invalid UPC non-numeric,SKU006,");

        // Invalid GTINs
        sb.AppendLine("GTIN,12345,Active,None,Invalid GTIN too short,SKU007,");
        sb.AppendLine("GTIN,123456789012345,Active,None,Invalid GTIN too long,SKU008,");

        // Invalid EANs
        sb.AppendLine("EAN,123456789012,Active,None,Invalid EAN 12 digits,SKU009,");
        sb.AppendLine("EAN,12345678901234,Active,None,Invalid EAN 14 digits,SKU010,");

        // Empty values
        sb.AppendLine("ASIN,,Active,Amazon,Empty ASIN value,SKU011,");
        sb.AppendLine(",B08N5WRWNW,Active,Amazon,Empty type,SKU012,");

        File.WriteAllText(path, sb.ToString());
    }

    private static void GenerateDuplicatesFile(string path)
    {
        Console.WriteLine($"Generating {path} with duplicate records...");

        var sb = new StringBuilder();
        sb.AppendLine("Type,Value,Status,Partner,ProductName,ProductSku,Description");

        // Intentional duplicates
        sb.AppendLine("ASIN,B0DUPLICATE1,Active,Amazon,First occurrence,SKU001,");
        sb.AppendLine("ASIN,B0DUPLICATE1,Active,Amazon,Duplicate occurrence,SKU002,");
        sb.AppendLine("UPC,012345678905,Active,None,First UPC,SKU003,");
        sb.AppendLine("UPC,012345678905,Inactive,Novi,Duplicate UPC different status,SKU004,");

        // Unique records
        for (int i = 0; i < 20; i++)
        {
            sb.AppendLine($"ASIN,B0UNIQUE{i:D4},Active,Amazon,Unique product {i},SKU{100 + i:D3},");
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void GenerateMixedValidityFile(string path)
    {
        Console.WriteLine($"Generating {path} with mixed valid/invalid records...");

        var sb = new StringBuilder();
        sb.AppendLine("Type,Value,Status,Partner,ProductName,ProductSku,Description");

        for (int i = 0; i < 100; i++)
        {
            if (i % 10 == 0) // 10% invalid
            {
                sb.AppendLine($"ASIN,INVALID{i},Active,Amazon,Invalid record {i},SKU{i:D3},");
            }
            else // 90% valid
            {
                var type = ProductIdTypes[_random.Next(ProductIdTypes.Length)];
                var value = GenerateValidProductId(type, i);
                sb.AppendLine($"{type},{value},Active,Amazon,Valid product {i},SKU{i:D3},");
            }
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static string GenerateValidProductId(string type, int index)
    {
        return type switch
        {
            "ASIN" => GenerateAsin(index),
            "UPC" => GenerateUpc(index),
            "GTIN" => GenerateGtin(index),
            "EAN" => GenerateEan(index),
            "ISBN" => GenerateIsbn(index),
            "SKU" => $"SKU-{index:D8}",
            "MPN" => $"MPN-{_random.Next(100, 999)}-{index:D6}",
            _ => $"ID{index:D10}"
        };
    }

    private static string GenerateAsin(int index)
    {
        var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var suffix = new char[8];
        var seed = index + 1000000;
        for (int i = 0; i < 8; i++)
        {
            suffix[i] = chars[(seed + i * 7) % chars.Length];
        }
        return $"B0{new string(suffix)}";
    }

    private static string GenerateUpc(int index)
    {
        var baseNumber = (100000000000 + index) % 100000000000;
        var upc = baseNumber.ToString("D11");
        var checkDigit = CalculateUpcCheckDigit(upc);
        return upc + checkDigit;
    }

    private static int CalculateUpcCheckDigit(string upc11)
    {
        var sum = 0;
        for (int i = 0; i < 11; i++)
        {
            var digit = int.Parse(upc11[i].ToString());
            sum += (i % 2 == 0) ? digit * 3 : digit;
        }
        return (10 - (sum % 10)) % 10;
    }

    private static string GenerateGtin(int index)
    {
        var baseNumber = (10000000000000 + index) % 10000000000000;
        return baseNumber.ToString("D14");
    }

    private static string GenerateEan(int index)
    {
        var baseNumber = (100000000000 + index) % 1000000000000;
        var ean = baseNumber.ToString("D12");
        var checkDigit = CalculateEanCheckDigit(ean);
        return ean + checkDigit;
    }

    private static int CalculateEanCheckDigit(string ean12)
    {
        var sum = 0;
        for (int i = 0; i < 12; i++)
        {
            var digit = int.Parse(ean12[i].ToString());
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        return (10 - (sum % 10)) % 10;
    }

    private static string GenerateIsbn(int index)
    {
        var baseNumber = (1000000000 + index) % 10000000000;
        return $"978{baseNumber:D10}";
    }
}
