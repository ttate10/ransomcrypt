using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Collections.Generic;

class Program2
{
    static string ComputeHash(string input)
    {
        using (var sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
    static bool VerifyPasswordHash(string password, string savedHash)
    {
        string computedHash = ComputeHash(password);
        return savedHash.Equals(computedHash);
    }



}