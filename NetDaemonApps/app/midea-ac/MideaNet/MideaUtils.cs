using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MideaAcIntegration.MideaNet
{
    public static class MideaUtils
    {
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

            string queryString = string.Join("&",
                form.OrderBy(i => i.Key)
                    .Select(x => x.Key + "=" + x.Value.ToString()));

            var source = postfix + queryString + MideaConstants.AppKey;

            using SHA256 sha256Hash = SHA256.Create();
            return GetHash(sha256Hash, source);
        }


        public static string GetSignPassword(string loginId, string password)
        {
            if (loginId == "" || password == "") return string.Empty;
            
            using SHA256 sha256Hash = SHA256.Create();
            string pw =  GetHash(sha256Hash, password);
            return GetHash(sha256Hash, loginId + pw + MideaConstants.AppKey);
        }

        public static string GenerateDataKey(string accessToken)
        {
            //To Verify:
            //token: 532471446b5e7b7e61c9f97fb8b2de9cd0a56da5a07373989bf6c3af1b7ca893
            //result: 9aa307eadde04289
            //md5key: 2976338e76d8e610e23c5925495548a5
            //slice: 2976338e76d8e610

            if (string.IsNullOrEmpty(accessToken)) return "";
            
            using MD5 sha256Hash = MD5.Create();
            string md5AppKey = GetHash(sha256Hash, MideaConstants.AppKey);
            //AesManaged
            var sub = md5AppKey.Substring(0, 16);
            return Decrypt(accessToken, sub);
        }
        
        //https://stackoverflow.com/questions/47441725/aes-128-bit-with-ecb-ciphermode-algorithm-decrypts-correctly-with-an-invalid-key
        static string Decrypt(string text, string keyStr)
        {
            //https://stackoverflow.com/a/27363456/452709
            var src = new byte[text.Length / 4];
            for (var i = 0; i < src.Length; i++)
            {
                src[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
            }


            Aes aes = Aes.Create();
            byte[] key = Encoding.ASCII.GetBytes(keyStr);
            aes.KeySize = 128;
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.ECB;
            using (ICryptoTransform decrypt = aes.CreateDecryptor(key, null))
            {
                byte[] dest = decrypt.TransformFinalBlock(src, 0, src.Length);
                decrypt.Dispose();
                return Encoding.UTF8.GetString(dest);
            }
        }
        
        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

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
}