using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class ParamKeyAttribute : Attribute
    {
        public string Key;
        public string Description;
        public ParamKeyAttribute(string key, string description = "")
        {
            Key = key;
            Description = description;
        }
    }
}
