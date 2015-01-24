using System;
using System.Management.Automation;
using System.Reflection;

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
    }
}
