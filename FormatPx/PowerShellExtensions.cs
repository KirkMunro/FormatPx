using System;
using System.Linq;
using System.Management.Automation;

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
    }
}
