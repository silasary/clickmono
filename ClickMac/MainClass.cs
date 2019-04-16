using Sentry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClickMac
{
    class MainClass
    {
        private static void Main(string[] args)
        {
            bool doSentry;
            
            try
            {
                doSentry = WithSentry.HasSentry();
            }
            catch (FileNotFoundException)
            {
                doSentry = false;
            }
            if (doSentry)
                WithSentry.DoMain(args);
            else
                Program.ActualMain(args);
        }
    }

    class WithSentry
    {
        internal static bool HasSentry()
        {
            return typeof(SentrySdk) != null;
        }
        internal static void DoMain(string[] args)
        {
            using (SentrySdk.Init((o) =>
            {
                o.Dsn = new Dsn("https://3db6919bd6c14b9a8a71cfa78c5ef1ef@sentry.io/1440030");
#if DEBUG
                o.Environment = "Debug";
#else
                o.Environment = "Release";
#endif
            }))
            {
                Program.ActualMain(args);
            }
        }
    }
}
