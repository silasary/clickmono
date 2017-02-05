using System;

namespace ClickMac
{
    internal class EntryPoint
    {
        public string DeploymentProviderUrl;

        public string executable;
        public string folder;
        public string version;
        public string icon;
        public string displayName;

        public void Import(EntryPoint child)
        {
            if (string.IsNullOrWhiteSpace(this.DeploymentProviderUrl))
                this.DeploymentProviderUrl = child.DeploymentProviderUrl;
            if (string.IsNullOrWhiteSpace(this.executable))
                this.executable = child.executable;
            if (string.IsNullOrWhiteSpace(this.folder))
                this.folder = child.folder;
            if (string.IsNullOrWhiteSpace(this.version))
                this.version = child.version;
            if (string.IsNullOrWhiteSpace(this.icon))
                this.icon = child.icon;
            if (string.IsNullOrWhiteSpace(this.displayName))
                this.displayName = child.displayName;
        }
    }
}

