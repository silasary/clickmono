using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Packager
{
    public class DeploymentOptions
    {
        public string ProviderUrl { get; internal set; } = string.Empty;
        public bool CreateDesktopShortcut { get; set; } = true;
        public bool Install { get; set; } = true;
        public TimeSpan MaximumAge { get; set; }
    }
}
