using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace FormatPx
{
    public class FormatContainer : DynamicObject
    {
        Dictionary<string, object> properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        static string[] supportedPropertyNames = new string[] { "Start", "End", "OobData" };

        public FormatContainer()
        {
            properties.Add("Start",   null);
            properties.Add("End",     null);
            properties.Add("OobData", null);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!supportedPropertyNames.Contains(binder.Name))
            {
                throw new ArgumentException(string.Format("FormatContainer objects only contain the following properties: Start, End and OobData. \"{0}\" is not in the list of valid property names", binder.Name));
            }

            result = properties[binder.Name];
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (!supportedPropertyNames.Contains(binder.Name))
            {
                throw new ArgumentException(string.Format("FormatContainer objects only contain the following properties: Start, End and OobData. \"{0}\" is not in the list of valid property names", binder.Name));
            }

            properties[binder.Name] = value;
            return true;
        }
    }
}
