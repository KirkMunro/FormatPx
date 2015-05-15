using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace FormatPx
{
    internal class ViewFinder
    {
        Dictionary<string, string> compatibleTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, PowerShellInternals.DefaultViewHelper> defaultViewMap = new Dictionary<string, PowerShellInternals.DefaultViewHelper>(StringComparer.OrdinalIgnoreCase);
        PowerShellInternals.DisplayDataQueryHelper displayDataQueryHelper;

        internal ViewFinder(PSCmdlet psCmdlet)
        {
            displayDataQueryHelper = new PowerShellInternals.DisplayDataQueryHelper(psCmdlet);
        }

        public PowerShellInternals.DefaultViewHelper GetDefaultView(PSObject psObject)
        {
            // Get the type hierarchy for the current object and convert it to a joined string
            List<string> typeHierarchies = new List<string>();
            List<string> typeNames = psObject.TypeNames.Select(x => Regex.Replace(x, @"^Deserialized\.", "")).ToList();
            for (int index = 0; index < typeNames.Count; index++)
            {
                typeHierarchies.Add(string.Join("|", typeNames.Skip(index)));
            }

            // Lookup the default format for the current object type
            foreach (string typeHierarchy in typeHierarchies)
            {
                if (!string.IsNullOrEmpty(typeHierarchy) && defaultViewMap.ContainsKey(typeHierarchy))
                {
                    return defaultViewMap[typeHierarchy];
                }
            }

            // If a default format was not found, look up the default format in PowerShell
            // and cache it for fast retrieval later
            PowerShellInternals.DefaultViewHelper defaultView = displayDataQueryHelper.GetDefaultView(psObject);
            if (!string.IsNullOrEmpty(defaultView.ViewTypeIdentifier) && defaultViewMap.ContainsKey(defaultView.ViewTypeIdentifier))
            {
                return defaultViewMap[defaultView.ViewTypeIdentifier];
            }
            foreach (string viewTypeName in defaultView.ViewTypeNames)
            {
                if (typeNames.Contains(viewTypeName))
                {
                    string typeHierarchy = string.Join("|", typeNames.SkipWhile(x => string.Compare(x, viewTypeName, true) != 0));
                    if (!string.IsNullOrEmpty(typeHierarchy) && !defaultViewMap.ContainsKey(typeHierarchy))
                    {
                        defaultViewMap.Add(typeHierarchy, defaultView);
                        if (!string.IsNullOrEmpty(defaultView.ViewTypeIdentifier) &&
                            !defaultView.ViewTypeNames.Contains(defaultView.ViewTypeIdentifier) &&
                            !defaultViewMap.ContainsKey(defaultView.ViewTypeIdentifier))
                        {
                            defaultViewMap.Add(defaultView.ViewTypeIdentifier, defaultView);
                        }
                    }
                    break;
                }
            }

            return defaultView;
        }
    }
}
