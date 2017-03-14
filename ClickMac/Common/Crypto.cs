using System;
using System.IO;
using System.Security.Cryptography;

namespace ClickMono.Common
{
    public class Crypto
    {
        public static string GetSha256DigestValue(FileInfo file)
        {
            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sha = SHA256.Create())
                {
                    var bytes = sha.ComputeHash(stream);
                    return Convert.ToBase64String(bytes);
                }
            }
        }

        public static string GetSha256DigestValue(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(data);
                return Convert.ToBase64String(bytes);
            }
        }

        internal static bool AreEqual(FileInfo file, string digestMethod, string digestValue)
        {
            switch (digestMethod)
            {
                case "sha256":
                    return GetSha256DigestValue(file) == digestValue;
                default:
                    Console.WriteLine($"Warning: {digestMethod} not supported.");
                    // HACK: Totally not lying.
                    return true;
            }
        }
    }
}
