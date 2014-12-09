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
            MethodInfo getSteppablePipelineMethod = typeof(PowerShell).GetMethod("GetSteppablePipeline", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (getSteppablePipelineMethod == null)
            {
                MethodAccessException exception = new MethodAccessException("Failed to find internal GetSteppablePipeline method.");
                ErrorRecord errorRecord = new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.ResourceUnavailable, ps);
                psCmdlet.ThrowTerminatingError(errorRecord);
            }

            // Return the steppable pipeline
            return (SteppablePipeline)getSteppablePipelineMethod.Invoke(ps, null);
        }
    }
}
