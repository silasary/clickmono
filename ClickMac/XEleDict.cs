using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ClickMac
{
    class XEleDict
    {
        public XElement inner;

        public static implicit operator XEleDict(XElement ele) { return new XEleDict(ele); }

        public XEleDict(XElement ele)
        {
            this.inner = ele;
        }

        public string this[string attrib]
        {
            get
            {
                var x = this.inner.Attribute(attrib);
                if (x != null)
                    return x.Value;
                return null;
            }
        }

    }
}
