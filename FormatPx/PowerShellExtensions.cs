using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace FormatPx
{
    internal static class PowerShellExtensions
    {
        internal static bool PropertyExists(this PSObject pso, string propertyName)
        {
            PSPropertyInfo propertyInfo = pso.Properties[propertyName];
            if (propertyInfo != null)
            {
                return (propertyInfo is PSAdaptedProperty ? pso.Properties.Any(x => string.Compare(x.Name, propertyName, true) == 0) : true);
            }
            return false;
        }

        internal static PSPropertyInfo GetPropertySafe(this PSObject pso, string propertyName)
        {
            PSPropertyInfo propertyInfo = pso.Properties[propertyName];
            if (propertyInfo is PSAdaptedProperty)
            {
                return (pso.Properties.Any(x => string.Compare(x.Name, propertyName, true) == 0) ? propertyInfo : null);
            }
            return propertyInfo;
        }

        internal static class ScalarTypeHelper
        {
            static HashSet<string> scalarTypeList;

            static ScalarTypeHelper()
            {
                scalarTypeList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                scalarTypeList.Add("System.String");
                scalarTypeList.Add("System.SByte");
                scalarTypeList.Add("System.Byte");
                scalarTypeList.Add("System.Int16");
                scalarTypeList.Add("System.UInt16");
                scalarTypeList.Add("System.Int32");
                scalarTypeList.Add("System.UInt32");
                scalarTypeList.Add("System.Int64");
                scalarTypeList.Add("System.UInt64");
                scalarTypeList.Add("System.Char");
                scalarTypeList.Add("System.Single");
                scalarTypeList.Add("System.Double");
                scalarTypeList.Add("System.Boolean");
                scalarTypeList.Add("System.Decimal");
                scalarTypeList.Add("System.IntPtr");
                scalarTypeList.Add("System.Security.SecureString");
                scalarTypeList.Add("System.Numerics.BigInteger");
            }

            internal static bool IsScalar(PSObject psObject)
            {
                if (psObject.TypeNames.Count == 0)
                {
                    return false;
                }

                string typeName = Regex.Replace(psObject.TypeNames[0], @"^Deserialized\.", "");
                if (psObject.TypeNames.Count > 1 && (string.Compare(Regex.Replace(psObject.TypeNames[1], @"^Deserialized\.", ""), "System.Enum", true) == 0))
                {
                    return true;
                }

                return scalarTypeList.Contains(typeName);
            }
        }

        internal static bool IsScalar(this PSObject psObject)
        {
            return ScalarTypeHelper.IsScalar(psObject);
        }
    }
}