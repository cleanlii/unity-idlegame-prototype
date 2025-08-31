using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
///     Encrypt and decrypt JSON data
/// </summary>
public static class JsonUtils
{
    private static readonly string EncryptionKey = EncryptionKeyManager.GetEncryptionKey(); // 必须为16字节的密钥

    /// <summary>
    ///     加密 JSON 数据并保存到文件
    /// </summary>
    public static void SaveEncryptedJson<T>(string filePath, T data, bool serialize = false)
    {
        try
        {
            var jsonData = serialize ? JsonConvert.SerializeObject(data, Formatting.Indented) : JsonUtility.ToJson(data, true);

            // 使用 AES 加密
            var encryptedData = Encrypt(jsonData, EncryptionKey);

            // 写入文件
            File.WriteAllText(filePath, encryptedData);

            Debug.Log($"文件已加密并保存到: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"加密文件失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     解密 JSON 文件并加载数据
    /// </summary>
    public static T LoadEncryptedJson<T>(string filePath, bool serialize = false)
    {
        try
        {
            // 读取加密文件内容
            if (!File.Exists(filePath))
            {
                Debug.LogError("文件不存在!");
                return default;
            }

            var encryptedData = File.ReadAllText(filePath);

            // 使用 AES 解密
            var decryptedData = Decrypt(encryptedData, EncryptionKey);

            // 将 JSON 转换为对象
            return serialize ? JsonConvert.DeserializeObject<T>(decryptedData) : JsonUtility.FromJson<T>(decryptedData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"解密文件失败: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    ///     AES 加密
    /// </summary>
    private static string Encrypt(string plainText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = new byte[16]; // 初始化向量，通常全 0 或随机值
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
        using (StreamWriter sw = new(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    ///     AES 解密
    /// </summary>
    private static string Decrypt(string cipherText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = new byte[16];
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        var buffer = Convert.FromBase64String(cipherText);

        using MemoryStream ms = new(buffer);
        using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
        using StreamReader sr = new(cs);

        return sr.ReadToEnd();
    }
}

public static class EncryptionKeyManager
{
    private static readonly string KeyFilePath = Path.Combine(Application.persistentDataPath, "TestKey.dat");

    public static string GetEncryptionKey()
    {
        if (!File.Exists(KeyFilePath))
        {
            var newKey = GenerateRandomKey(16);
            File.WriteAllText(KeyFilePath, newKey);
            return newKey;
        }

        return File.ReadAllText(KeyFilePath);
    }

    private static string GenerateRandomKey(int length)
    {
        var rng = new RNGCryptoServiceProvider();
        var keyBytes = new byte[length];
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes).Substring(0, length);
    }
}