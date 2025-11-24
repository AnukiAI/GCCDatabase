#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

string password = Args.Count > 0 ? Args[0] : "TestPass123!";
string hash = BCrypt.HashPassword(password);

Console.WriteLine($"Password: {password}");
Console.WriteLine($"BCrypt Hash: {hash}");
Console.WriteLine();
Console.WriteLine("SQL UPDATE for admin user:");
Console.WriteLine($"UPDATE dbo.Users SET PasswordHash = '{hash}' WHERE Username = 'admin';");
Console.WriteLine();
Console.WriteLine("Verification test:");
bool isValid = BCrypt.Verify(password, hash);
Console.WriteLine($"Verify('{password}', hash) = {isValid}");
