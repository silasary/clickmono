using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ClickMac
{
    internal class CurlWrapper : IDisposable
    {
        public void Dispose()
        {
        }
#if CurlSharp
        static bool _initialized = false;

        CurlEasy curl;
        MemoryStream stream;

        public CurlWrapper()
        {
            if (!_initialized)
                Curl.GlobalInit(CurlInitFlag.All);
            curl = new CurlEasy();
        }

        public byte[] Get(string Url)
        {
            GetStream(Url);
            byte[] content = new byte[stream.Length];
            stream.Read(content, 0, (int)stream.Length);
            return content;
        }

        public Stream GetStream(string Url)
        {
            stream = new MemoryStream();
            curl.Url = Url;
            curl.WriteData = null;
            curl.WriteFunction = WriteFunc;
            curl.HeaderFunction = HeadFunc;
            curl.Perform();
            return stream;
        }

        private int HeadFunc(byte[] buf, int size, int nmemb, object extraData)
        {
            Console.Write(Encoding.UTF8.GetString(buf));
            return size * nmemb;
        }

        private int WriteFunc(byte[] buf, int size, int nmemb, object extraData)
        {
            stream.Write(buf, 0, size * nmemb);
            return size * nmemb;
        }

        void IDisposable.Dispose()
        {
            stream.Dispose();
            curl.Dispose();
        }
#endif
        string curlPath = "curl";

        public FileInfo GetFile(string Url)
        {
            var tmp = Path.GetTempFileName();
            try
            {
                // HACK: -k disables TLS verification.  We should instead load the mozroots cert store.
                // !!!!!FIX THIS ASAP!!!!!
                var psi = new ProcessStartInfo(curlPath, $"-k {Url} -o {tmp}")
                {
                    UseShellExecute = false
                };
                Process.Start(psi).WaitForExit();
            }
            catch (Win32Exception) when (curlPath == "curl")
            {
                File.Delete(tmp);
                curlPath = Path.Combine(Path.GetDirectoryName(Program.Location), "curl.exe");
                return GetFile(Url);
            }
            return new FileInfo(tmp);
        }
    }
}