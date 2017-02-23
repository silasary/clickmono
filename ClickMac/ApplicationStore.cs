using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClickMac
{
    static class ApplicationStore
    {
        public static void Install(Manifest manifest)
        {
            var dest = Path.Combine(Platform.LibraryLocation, "Manifests", manifest.Identity + ".application");
            manifest.Xml.Save(dest);

        }

        public static void Uninstall(Manifest manifest)
        {
            Uninstall(manifest.Identity);
        }

        public static void Uninstall(string identity)
        {
            var m = Path.Combine(Platform.LibraryLocation, "Manifests", identity + ".application");
            if (File.Exists(m))
                File.Delete(m);

        }

        private static void Cleanup()
        {
            // TODO [6] : Delete all bar the latest two versions of each installed app, and remove all unused dependancies.
            // This will prevent unneeded disk bloating, and prevent buildup of too many old versions.
            // It will also allow for true uninstallation (As compared to what currently happens)
        }
    }
}
