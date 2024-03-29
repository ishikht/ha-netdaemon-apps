﻿using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MideaAcIntegration.MideaNet;

public static class MideaUtils
{
    public static int CalculateCrc8(int[] data)
    {
        var crcValue = 0;
        foreach (var m in data)
        {
            var k = crcValue ^ m;
            if (k > 256) k -= 256;
            if (k < 0) k += 256;
            crcValue = MideaConstants.Crc8Table854[k];
        }

        return crcValue;
    }

    public static int[] Encode(int[] data)
    {
        var normalized = new List<int>();
        foreach (var b in data)
        {
            var rb = b;
            if (b >= 128) rb = b - 256;
            normalized.Add(rb);
        }

        return normalized.ToArray();
    }

    public static int[] Decode(int[] data)
    {
        var normalized = new List<int>();
        foreach (var b in data)
        {
            var rb = b;
            if (b < 0) rb = b + 256;
            normalized.Add(rb);
        }

        return normalized.ToArray();
    }

    public static string GetStamp()
    {
        return DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
    }

    public static string GetSign(string path, IDictionary<string, string> form)
    {
        if (form == null) throw new ArgumentNullException(nameof(form));
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path can't be empty", nameof(path));

        var postfix = "/v1" + path;
        postfix = postfix.Split('?')[0];

        var queryString = string.Join("&",
            form.OrderBy(i => i.Key)
                .Select(x => x.Key + "=" + x.Value.ToString()));

        var source = postfix + queryString + MideaConstants.AppKey;

        using var sha256Hash = SHA256.Create();
        return GetHash(sha256Hash, source);
    }


    public static string GetSignPassword(string loginId, string password)
    {
        if (loginId == "" || password == "") return string.Empty;

        using var sha256Hash = SHA256.Create();
        var pw = GetHash(sha256Hash, password);
        return GetHash(sha256Hash, loginId + pw + MideaConstants.AppKey);
    }

    public static string GenerateDataKey(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return "";

        using var sha256Hash = MD5.Create();
        var md5AppKey = GetHash(sha256Hash, MideaConstants.AppKey);
        //AesManaged
        var sub = md5AppKey.Substring(0, 16);
        return Decrypt(accessToken, sub, true);
    }

    public static string EncryptAes(int[] query, string dataKey)
    {
        if (!query.Any() || dataKey == "") return string.Empty;

        var queryText = string.Join(",", query);
        return Encrypt(queryText, dataKey);
    }

    public static int[]? DecryptAes(string reply, string dataKey)
    {
        if (string.IsNullOrEmpty(reply) || string.IsNullOrEmpty(dataKey)) return null;

        var result = Decrypt(reply, dataKey);
        return result.Split(",").Select(s =>
        {
            var isSucceeded = int.TryParse(s, out var i);
            return isSucceeded ? i : 0;
        }).ToArray();
    }

    //https://stackoverflow.com/questions/47441725/aes-128-bit-with-ecb-ciphermode-algorithm-decrypts-correctly-with-an-invalid-key
    private static string Decrypt(string text, string keyStr, bool smallBlock = false)
    {
        var src = FromHexString(text, smallBlock);

        var aes = Aes.Create();
        var key = Encoding.ASCII.GetBytes(keyStr);
        aes.KeySize = 128;
        aes.Padding = PaddingMode.None;
        aes.Mode = CipherMode.ECB;

        using var decrypt = aes.CreateDecryptor(key, null);
        var dest = decrypt.TransformFinalBlock(src, 0, src.Length);
        decrypt.Dispose();

        return Encoding.UTF8.GetString(dest);
    }

    //https://stackoverflow.com/questions/47441725/aes-128-bit-with-ecb-ciphermode-algorithm-decrypts-correctly-with-an-invalid-key
    private static string Encrypt(string text, string keyStr)
    {
        var src = Encoding.UTF8.GetBytes(text);
        var key = Encoding.ASCII.GetBytes(keyStr);
        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 128;

        using var encrypt = aes.CreateEncryptor(key, null);

        var dest = encrypt.TransformFinalBlock(src, 0, src.Length);
        encrypt.Dispose();

        return ToHexString(dest);
    }

    //https://stackoverflow.com/a/27363456/452709
    private static byte[] FromHexString(string text, bool smallBlock = false)
    {
        var src = new byte[text.Length / (2 * (smallBlock ? 2 : 1))];
        for (var i = 0; i < src.Length; i++)
            src[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);

        return src;
    }

    //https://stackoverflow.com/a/27363456/452709
    private static string ToHexString(byte[] data)
    {
        var sb = new StringBuilder();

        foreach (var t in data) sb.Append(t.ToString("X2"));

        return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
    }

    private static string GetHash(HashAlgorithm hashAlgorithm, string input)
    {
        // Convert the input string to a byte array and compute the hash.
        var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        var sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data
        // and format each one as a hexadecimal string.
        foreach (var b in data)
            sBuilder.Append(b.ToString("x2"));

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }
}