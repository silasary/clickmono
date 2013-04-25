using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kamahl.Deployment
{
    partial class ApplicationDeployment // : IApplicationDeployment
    {
        /// <summary>Returns the current <see cref="T:Kamahl.Deployment.ApplicationDeployment" /> for this deployment.</summary>
        public static ApplicationDeployment CurrentDeployment { get; internal set; }
        /// <summary> Gets a value indicating whether the current application is a ClickOnce application. </summary>
        public static bool IsNetworkDeployed { get { return CurrentDeployment != null; } }

        internal ApplicationDeployment(Uri ActivationUri, Version version)
        {
            this.ActivationUri = ActivationUri;
            this.CurrentVersion = version;
        }
        public Uri ActivationUri { get; private set; }

        public Version CurrentVersion { get; private set; }

        public string DataDirectory
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsFirstRun
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime TimeOfLastUpdateCheck
        {
            get { throw new NotImplementedException(); }
        }

        public string UpdatedApplicationFullName
        {
            get { throw new NotImplementedException(); }
        }

        public Version UpdatedVersion
        {
            get { throw new NotImplementedException(); }
        }

        public Uri UpdateLocation
        {
            get { throw new NotImplementedException(); }
        }
    }
}