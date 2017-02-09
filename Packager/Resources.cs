using AsmResolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Packager
{
    public class Resources
    {
        public string Executable { get; private set; }
        public WindowsAssembly Assembly { get; private set; }

        public IEnumerable<ImageResourceDirectoryEntry> GetResourcesOf(ImageResourceDirectoryType t, ImageResourceDirectory dir = null)
        {
            if (dir == null)
                dir = Assembly.RootResourceDirectory;
            foreach (var item in dir.Entries)
            {
                if (item.ResourceType == t)
                {
                    yield return item;
                }
            }
            yield break;
        }

        public Resources(string exe)
        {
            this.Executable = exe;
            Assembly = WindowsAssembly.FromFile(exe);
        }

        public void StripManifest()
        {
            
            foreach (var res in GetResourcesOf(ImageResourceDirectoryType.Configuration))
            {
                
            }
        }

        public ManifestFile ExtractIcon(string projectexe)
        {
            return null;
            // This is all gross.  Just ask the user to include an icon as content.
        }

    }
}
