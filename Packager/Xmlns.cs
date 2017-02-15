using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Packager
{
    static class Xmlns
    {
        public static XNamespace asmv1ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v1");
        public static XNamespace asmv2ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v2");
        public static XNamespace asmv3ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v3");
        public static XNamespace clickoncev1ns = XNamespace.Get("urn:schemas-microsoft-com:clickonce.v1");
        public static XNamespace clickoncev2ns = XNamespace.Get("urn:schemas-microsoft-com:clickonce.v2");
        public static XNamespace dsigns = XNamespace.Get("http://www.w3.org/2000/09/xmldsig#");


        public static XName asmv1assembly = asmv1ns.GetName("assembly");
        public static XName asmv1assemblyIdentity = asmv1ns.GetName("assemblyIdentity");
        public static XName asmv1name = asmv1ns.GetName("name");
        public static XName asmv1version = asmv1ns.GetName("version");
        public static XName asmv1publicKeyToken = asmv1ns.GetName("publicKeyToken");
        public static XName asmv1language = asmv1ns.GetName("language");
        public static XName asmv1processorArchitecture = asmv1ns.GetName("processorArchitecture");
        public static XName asmv1description = asmv1ns.GetName("description");

        public static XName asmv2publisher = asmv2ns.GetName("publisher");
        public static XName asmv2deployment = asmv2ns.GetName("deployment");
        public static XName asmv2product = asmv2ns.GetName("product");
        public static XName asmv2install = asmv2ns.GetName("install");
        public static XName asmv2mapFileExtensions = asmv2ns.GetName("mapFileExtensions");
        public static XName asmv2subscription = asmv2ns.GetName("subscription");
        public static XName asmv2update = asmv2ns.GetName("update");
        public static XName asmv2beforeApplicationStartup = asmv2ns.GetName("beforeApplicationStartup");
        public static XName asmv2deploymentProvider = asmv2ns.GetName("deploymentProvider");
        public static XName asmv2codebase = asmv2ns.GetName("codebase");
        public static XName asmv2dependency = asmv2ns.GetName("dependency");
        public static XName asmv2dependentAssembly = asmv2ns.GetName("dependentAssembly");
        public static XName asmv2dependencyType = asmv2ns.GetName("dependencyType");
        public static XName asmv2size = asmv2ns.GetName("size");
        public static XName asmv2assemblyIdentity = asmv2ns.GetName("assemblyIdentity");
        public static XName asmv2name = asmv2ns.GetName("name");
        public static XName asmv2version = asmv2ns.GetName("version");
        public static XName asmv2publicKeyToken = asmv2ns.GetName("publicKeyToken");
        public static XName asmv2language = asmv2ns.GetName("language");
        public static XName asmv2processorArchitecture = asmv2ns.GetName("processorArchitecture");
        public static XName asmv2type = asmv2ns.GetName("type");
        public static XName asmv2hash = asmv2ns.GetName("hash");
        public static XName asmv2iconFile = asmv2ns.GetName("iconFile");

        public static XName clickoncev2compatibleFrameworks = clickoncev2ns.GetName("compatibleFrameworks");
        public static XName clickoncev2framework = clickoncev2ns.GetName("framework");
        public static XName clickoncev2targetVersion = clickoncev2ns.GetName("targetVersion");
        public static XName clickoncev2profile = clickoncev2ns.GetName("profile");
        public static XName clickoncev2supportedRuntime = clickoncev2ns.GetName("supportedRuntime");

        public static XName dsigTransforms = dsigns.GetName("Transforms");
        public static XName dsigTransform = dsigns.GetName("Transform");
        public static XName dsigAlgorithm = XName.Get("Algorithm");// dsigns.GetName("Algorithm");
        public static XName dsigDigestMethod = dsigns.GetName("DigestMethod");
        public static XName dsigDigestValue = dsigns.GetName("DigestValue");


        public static XName asmv2application = asmv2ns.GetName("application");
        public static XName asmv2entryPoint = asmv2ns.GetName("entryPoint");
        public static XName asmv2trustInfo = asmv2ns.GetName("trustInfo");
        public static XName asmv2security = asmv2ns.GetName("security");
        public static XName asmv2applicationRequestMinimum = asmv2ns.GetName("applicationRequestMinimum");
        public static XName asmv2PermissionSet = asmv2ns.GetName("PermissionSet");
        public static XName asmv2defaultAssemblyRequest = asmv2ns.GetName("defaultAssemblyRequest");
        public static XName asmv2file = asmv2ns.GetName("file");
        public static XName asmv2commandLine = asmv2ns.GetName("commandLine");
        public static XName asmv2dependentOS = asmv2ns.GetName("dependentOS");
        public static XName asmv2osVersionInfo = asmv2ns.GetName("osVersionInfo");
        public static XName asmv2os = asmv2ns.GetName("os");
        public static XName asmv3requestedPrivileges = asmv3ns.GetName("requestedPrivileges");
        public static XName asmv3requestedExecutionLevel = asmv3ns.GetName("requestedExecutionLevel");

        public static XName clickoncev1useManifestForTrust = clickoncev1ns.GetName("useManifestForTrust");
        public static XName clickoncev1createDesktopShortcut = clickoncev1ns.GetName("createDesktopShortcut");
    }
}
