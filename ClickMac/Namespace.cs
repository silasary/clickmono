using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ClickMac
{
    /// <summary>
    /// Namespaces
    /// </summary>
    enum ns
    {
        /// <summary>
        /// urn:schemas-microsoft-com:asm.v1
        /// </summary>
        asmv1,
 
        /// <summary>
        /// urn:schemas-microsoft-com:asm.v2
        /// </summary>
        asmv2,
        
        /// <summary>
        /// urn:schemas-microsoft-com:asm.v3
        /// </summary>
        asmv3,
        
        /// <summary>
        /// urn:schemas-microsoft-com:clickonce.v1
        /// </summary>
        cov1,
        
        /// <summary>
        /// urn:schemas-microsoft-com:clickonce.v2
        /// </summary>
        cov2,
        
        /// <summary>
        /// http://www.w3.org/2000/09/xmldsig#
        /// </summary>
        dsig
    }
    class Namespace
    {
        public static XName XName(string localName, ns Ns)
        {
            string NameSpace = "";
            switch (Ns)
            {
                case ns.asmv1:
                    NameSpace = "urn:schemas-microsoft-com:asm.v1";
                    break;
                case ns.asmv2:
                    NameSpace = "urn:schemas-microsoft-com:asm.v2";
                    break;
                case ns.asmv3:
                    NameSpace = "urn:schemas-microsoft-com:asm.v3";
                    break;
                case ns.cov1:
                    NameSpace = "urn:schemas-microsoft-com:clickonce.v1";
                    break;
                case ns.cov2:
                    NameSpace = "urn:schemas-microsoft-com:clickonce.v2";
                    break;
                case ns.dsig:
                    NameSpace = "http://www.w3.org/2000/09/xmldsig#";
                    break;
            }
            return System.Xml.Linq.XName.Get(localName, NameSpace);
        }
    }
}