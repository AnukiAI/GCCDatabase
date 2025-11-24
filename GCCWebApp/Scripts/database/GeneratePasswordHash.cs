// Quick C# script to generate BCrypt password hash
// Run with: dotnet script GeneratePasswordHash.cs
// Or paste into https://dotnetfiddle.net/

using System;

public class PasswordHashGenerator
{
    public static void Main()
    {
        string password = "TestPass123!";

        // Install BCrypt.Net-Next package first
        // dotnet add package BCrypt.Net-Next

        string hash = BCrypt.Net.BCrypt.HashPassword(password);

        Console.WriteLine("Password: " + password);
        Console.WriteLine("BCrypt Hash: " + hash);
        Console.WriteLine();
        Console.WriteLine("SQL UPDATE statement:");
        Console.WriteLine($"UPDATE dbo.Users SET PasswordHash = '{hash}' WHERE Username = 'admin';");
    }
}
