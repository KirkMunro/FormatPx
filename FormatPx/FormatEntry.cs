using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace FormatPx
{
    public class FormatEntry : DynamicObject
    {
        Dictionary<string, object> properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        static string[] supportedPropertyNames = new string[] { "Data", "OobData" };

        public FormatEntry()
        {
            properties.Add("Data",    null);
            properties.Add("OobData", null);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!supportedPropertyNames.Contains(binder.Name))
            {
                throw new ArgumentException(string.Format("FormatEntry objects only contain the following properties: Data and OobData. \"{0}\" is not in the list of valid property names", binder.Name));
            }

            result = properties[binder.Name];
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (!supportedPropertyNames.Contains(binder.Name))
            {
                throw new ArgumentException(string.Format("FormatEntry objects only contain the following properties: Data and OobData. \"{0}\" is not in the list of valid property names", binder.Name));
            }

            properties[binder.Name] = value;
            return true;
        }
    }
}