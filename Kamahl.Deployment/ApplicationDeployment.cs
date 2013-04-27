using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kamahl.Deployment
{
    public partial class ApplicationDeployment // : IApplicationDeployment
    {
        private static ApplicationDeployment _current = null;
        /// <summary>Returns the current <see cref="T:Kamahl.Deployment.ApplicationDeployment" /> for this deployment.</summary>
        public static ApplicationDeployment CurrentDeployment
        {
            get
            {
                if (_current == null)
                {
                    var ad = AppDomain.CurrentDomain;
                    if (ad.GetData<bool>("Kamahl.Deployment.Deployed") == false)
                        return null;
                    _current = new ApplicationDeployment(ad.GetData<Uri>("Kamahl.Deployment.ActivationUri"), ad.GetData<Version>("Kamahl.Deployment.Version"));
                }
                return _current;
            }
        }
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