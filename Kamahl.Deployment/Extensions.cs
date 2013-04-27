using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kamahl.Deployment
{
    public static class Extensions
    {
        public static T GetData<T>(this AppDomain ad, string name)
        {
            return (T)ad.GetData(name);
        }

    }
}
