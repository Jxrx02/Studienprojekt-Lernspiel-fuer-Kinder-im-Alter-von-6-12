using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SecurityUtils
{
    // Device-dependent encryption key to make it harder to extract on different devices
    private static string GetDeviceKey()
    {
        return SystemInfo.deviceUniqueIdentifier + SystemInfo.deviceModel + SystemInfo.processorType;
    }
    
    // Simple encryption for storing sensitive data
    public static string Encrypt(string clearText)
    {
        if (string.IsNullOrEmpty(clearText))
        {
            return string.Empty;
        }
        
        try
        {
            string deviceKey = GetDeviceKey();
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            
            using (Aes encryptor = Aes.Create())
            {
                // Create key and IV from device key
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(
                    deviceKey,
                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            
            return clearText;
        }
        catch (Exception e)
        {
            Debug.LogError("Error encrypting data: " + e.Message);
            return string.Empty;
        }
    }
    
    // Decrypt data
    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }
        
        try
        {
            string deviceKey = GetDeviceKey();
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(
                    deviceKey,
                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            
            return cipherText;
        }
        catch (Exception e)
        {
            Debug.LogError("Error decrypting data: " + e.Message);
            return string.Empty;
        }
    }
    
    // Create a one-way hash of sensitive data (for comparisons)
    public static string Hash(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return string.Empty;
        }
        
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(data));
            
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
} 