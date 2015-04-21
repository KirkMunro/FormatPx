﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FormatPx
{
    internal static class PowerShellInternals
    {
        internal static SteppablePipeline GetSteppablePipeline(this PowerShell ps, PSCmdlet psCmdlet)
        {
            // The PowerShell language itself supports getting steppable pipelines from a script block,
            // however this support inside of compiled cmdlets is hidden behind internal methods. Since
            // proxying commands allows for powerful extension support in PowerShell, and since cmdlets
            // perform much better than their PowerShell function equivalent, I decided to expose the
            // internal method that is required via an extension method on a PowerShell object. This
            // extension method can be included in any project where it is needed.

            // Look up the GetSteppablePipeline internal method
            MethodInfo getSteppablePipelineMethod = typeof(PowerShell).GetMethod("GetSteppablePipeline", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (getSteppablePipelineMethod == null)
            {
                MethodAccessException exception = new MethodAccessException("Failed to find GetSteppablePipeline method.");
                ErrorRecord errorRecord = new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.ResourceUnavailable, ps);
                psCmdlet.ThrowTerminatingError(errorRecord);
            }

            // Return the steppable pipeline
            return (SteppablePipeline)getSteppablePipelineMethod.Invoke(ps, null);
        }

        internal static void Hide(this PSNoteProperty psNoteProperty, PSCmdlet psCmdlet)
        {
            // When adding the __FormatData property, we really don't want to see it in any
            // format output (Format-Table, Format-List). That makes this solution much more
            // magical. Plus, types that do not have a default display property set end up
            // showing __FormatData in the output in several use cases (such as piping to
            // ft -auto), and this is undesirable because it makes the solution much less
            // transparent. Better to just hide the field altogether and make sure that it
            // doesn't show up in the output at all.

            // Look up the isHidden value, first using the internal field, then using the property
            FieldInfo isHiddenField = typeof(PSNoteProperty).GetField("isHidden", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (isHiddenField == null)
            {
                // In PSv5+, the isHidden field is not accessible this way, but the IsHidden property
                // is so when the field is not found, we try using the property instead. We don't use
                // the property first because the property does not have a set accessor in v3 or v4.
                PropertyInfo isHiddenProperty = typeof(PSNoteProperty).GetProperty("IsHidden", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (isHiddenProperty == null)
                {
                    // When neither the field nor the property can be found, raise an exception.
                    FieldAccessException exception = new FieldAccessException("Failed to find isHidden field.");
                    ErrorRecord errorRecord = new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.ResourceUnavailable, psNoteProperty);
                    psCmdlet.ThrowTerminatingError(errorRecord);
                }
                else
                {
                    // Now hide the PSNoteProperty
                    isHiddenProperty.SetValue(psNoteProperty, true);
                }
            }
            else
            {
                // Now hide the PSNoteProperty
                isHiddenField.SetValue(psNoteProperty, true);
            }
        }

        internal static bool GetIsHidden(this PSMemberInfo psMemberInfo, PSCmdlet psCmdlet = null)
        {
            // Look up the isHidden value, first using the internal field, then using the property
            FieldInfo isHiddenField = typeof(PSNoteProperty).GetField("isHidden", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (isHiddenField == null)
            {
                // In PSv5+, the isHidden field is not accessible this way, but the IsHidden property
                // is so when the field is not found, we try using the property instead. We don't use
                // the property first because the property does not have a set accessor in v3 or v4.
                PropertyInfo isHiddenProperty = typeof(PSNoteProperty).GetProperty("IsHidden", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (isHiddenProperty == null)
                {
                    // When neither the field nor the property can be found, raise an exception.
                    FieldAccessException exception = new FieldAccessException("Failed to find isHidden field.");
                    if (psCmdlet == null)
                    {
                        throw exception;
                    }
                    else
                    {
                        ErrorRecord errorRecord = new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.ResourceUnavailable, psMemberInfo);
                        psCmdlet.ThrowTerminatingError(errorRecord);
                    }
                }
                else
                {
                    // Now get the hidden flag value
                    return (bool)isHiddenProperty.GetValue(psMemberInfo);
                }
            }
            else
            {
                // Now get the hidden flag value
                return (bool)isHiddenField.GetValue(psMemberInfo);
            }

            // If we can't find the appropriate property/field, assume it is not hidden.
            return false;
        }

        internal static PSMemberSet GetStandardMembers(this PSObject psObject)
        {
            PropertyInfo psStandardMembersProperty = psObject.GetType().GetProperty("PSStandardMembers", BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance);
            if (psStandardMembersProperty == null)
            {
                return null;
            }

            return (PSMemberSet)psStandardMembersProperty.GetValue(psObject);
        }

        internal static PSPropertySet GetDefaultDisplayPropertySet(this PSObject psObject)
        {
            PSMemberSet psStandardMembers = psObject.GetStandardMembers();
            if (psStandardMembers == null)
            {
                return null;
            }
            return psStandardMembers.Members["DefaultDisplayPropertySet"] as PSPropertySet;
        }

        internal class DefaultViewHelper
        {
            object defaultView = null;
            bool outOfBandView = false;
            Type mainControlType = null;
            PSObject psObject = null;
            PSPropertySet defaultDisplayPropertySet = null;
            string signature = "";

            internal DefaultViewHelper(PSObject psObject)
            {
                this.psObject = psObject;
                signature = psObject.TypeNames.Count == 0 ? psObject.GetType().FullName : Regex.Replace(psObject.TypeNames[0], @"^Deserialized\.", "");
                if (!psObject.IsScalar())
                {
                    defaultDisplayPropertySet = psObject.GetDefaultDisplayPropertySet();
                    if (defaultDisplayPropertySet != null)
                    {
                        signature = string.Format("{0}|{1}", signature, string.Join("|", defaultDisplayPropertySet.ReferencedPropertyNames));
                    }
                    else
                    {
                        signature = string.Format("{0}|{1}", signature, string.Join("|", psObject.Properties.Where(x => !x.GetIsHidden()).Select(x => x.Name)));
                    }
                }
                else
                {
                    signature = string.Format("{0}|{1}", signature, psObject.ToString());
                }
            }

            internal DefaultViewHelper(object defaultView, bool outOfBandView)
            {
                this.defaultView = defaultView;
                this.outOfBandView = outOfBandView;
                var mainControlField = defaultView.GetType().GetField("mainControl", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var mainControl = mainControlField.GetValue(defaultView);
                mainControlType = mainControl.GetType();
            }

            internal string GetViewType()
            {
                if (mainControlType != null)
                {
                    if (string.Compare(mainControlType.Name, "TableControlBody", true) == 0)
                    {
                        return "Table";
                    }
                    else if (string.Compare(mainControlType.Name, "ListControlBody", true) == 0)
                    {
                        return "List";
                    }
                    else if (string.Compare(mainControlType.Name, "WideControlBody", true) == 0)
                    {
                        return "Wide";
                    }

                    return "Custom";
                }
                else if (psObject != null)
                {
                    if (psObject.IsScalar())
                    {
                        return "Scalar";
                    }

                    PSPropertySet defaultDisplayPropertySet = psObject.GetDefaultDisplayPropertySet();
                    if (((defaultDisplayPropertySet != null) &&
                         (defaultDisplayPropertySet.ReferencedPropertyNames.Count < 5)) ||
                        (psObject.Properties.Where(x => !x.GetIsHidden()).Count() < 5))
                    {
                        return "Table";
                    }

                    return "List";
                }

                return "Unknown";
            }

            public override int GetHashCode()
            {
                if (defaultView != null)
                {
                    return defaultView.GetHashCode();
                }

                return signature.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                DefaultViewHelper defaultViewHelper = obj as DefaultViewHelper;
                if (defaultViewHelper == null)
                {
                    return false;
                }
                return GetHashCode() == defaultViewHelper.GetHashCode();
            }
        }

        internal class DisplayDataQueryHelper
        {
            object expressionFactory = null;
            object typeInfoDataBase = null;
            MethodInfo getDefaultViewMethod = null;
            MethodInfo getOutOfBandViewMethod = null;

            internal DisplayDataQueryHelper(PSCmdlet psCmdlet)
            {
                Assembly smaAssembly = typeof(PowerShell).Assembly;

                Type mshExpressionFactoryType = smaAssembly.GetType("Microsoft.PowerShell.Commands.Internal.Format.MshExpressionFactory");
                var mshExpressionFactoryConstructor = mshExpressionFactoryType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                expressionFactory = mshExpressionFactoryConstructor.Invoke(null);

                var contextProperty = psCmdlet.GetType().GetProperty("Context", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var context = contextProperty.GetValue(psCmdlet);
                var formatDBManagerProperty = context.GetType().GetProperty("FormatDBManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var formatDBManager = formatDBManagerProperty.GetValue(context);
                var getTypeInfoDatabaseMethod = formatDBManager.GetType().GetMethod("GetTypeInfoDataBase", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                typeInfoDataBase = getTypeInfoDatabaseMethod.Invoke(formatDBManager, null);
                Type displayDataQueryType = smaAssembly.GetType("Microsoft.PowerShell.Commands.Internal.Format.DisplayDataQuery");
                getDefaultViewMethod = displayDataQueryType.GetMethod("GetDefaultView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { expressionFactory.GetType(), typeInfoDataBase.GetType(), typeof(Collection<string>) }, null);
                getOutOfBandViewMethod = displayDataQueryType.GetMethod("GetOutOfBandView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { expressionFactory.GetType(), typeInfoDataBase.GetType(), typeof(Collection<string>) }, null);
            }

            public DefaultViewHelper GetDefaultView(PSObject psObject)
            {
                Collection<string> typeNames = new Collection<string>();
                foreach (string typeName in psObject.TypeNames.Select(x => Regex.Replace(x, @"^Deserialized\.", "")))
                {
                    typeNames.Add(typeName);
                }
                bool outOfBandView = false;
                var defaultView = getDefaultViewMethod.Invoke(null, new object[] { expressionFactory, typeInfoDataBase, typeNames });
                if (defaultView == null)
                {
                    defaultView = getOutOfBandViewMethod.Invoke(null, new object[] { expressionFactory, typeInfoDataBase, typeNames });
                    outOfBandView = defaultView != null;
                }
                if (defaultView != null)
                {
                    return new DefaultViewHelper(defaultView, outOfBandView);
                }

                return new DefaultViewHelper(psObject);
            }
        }

        internal static DisplayDataQueryHelper GetDisplayDataQueryHelper(this PSCmdlet psCmdlet)
        {
            return new DisplayDataQueryHelper(psCmdlet);
        }
    }
}