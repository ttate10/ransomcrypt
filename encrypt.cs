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
}