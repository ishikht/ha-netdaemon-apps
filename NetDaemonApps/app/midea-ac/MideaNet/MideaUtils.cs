using System;
using System.Collections.Generic;
using System.Globalization;
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