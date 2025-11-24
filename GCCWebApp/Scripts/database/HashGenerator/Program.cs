using System;

namespace HashGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string password = args.Length > 0 ? args[0] : "TestPass123!";
            string hash = BCrypt.Net.BCrypt.HashPassword(password);

            Console.WriteLine($"Password: {password}");
            Console.WriteLine($"BCrypt Hash: {hash}");
            Console.WriteLine();
            Console.WriteLine("SQL UPDATE statement:");
            Console.WriteLine($"UPDATE dbo.Users SET PasswordHash = '{hash}' WHERE Username = 'admin';");
            Console.WriteLine();

            // Verify it works
            bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
            Console.WriteLine($"Verification test: {isValid}");
        }
    }
}
