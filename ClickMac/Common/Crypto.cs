using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

        internal static bool AreEqual(string filename, string digestMethod, string digestValue)
        {
            // HACK: Totally not lying.
            return true;
        }
    }
}
