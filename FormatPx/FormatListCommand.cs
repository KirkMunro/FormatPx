using System;
using System.Management.Automation;

namespace FormatPx
{
    [Cmdlet(
        VerbsCommon.Format,
        "List",
        HelpUri = "http://go.microsoft.com/fwlink/p/?linkid=293961"
    )]
    [OutputType(typeof(object))]
    public class FormatListCommand : Microsoft.PowerShell.Commands.FormatListCommand
    {
        [Parameter(
            HelpMessage = "Persists the format data on the object when it is output. By default, format data is discarded when output."
        )]
        public SwitchParameter PersistWhenOutput = false;

        FormatProxyCmdletHelper formatProxyHelper = null;

        protected override void BeginProcessing()
        {
            // Remove any non-core parameters before invoking the proxy command target
            if (MyInvocation.BoundParameters.ContainsKey("PersistWhenOutput"))
            {
                MyInvocation.BoundParameters.Remove("PersistWhenOutput");
            }
            // Use the proxy cmdlet helper to keep the code dry
            formatProxyHelper = new FormatProxyCmdletHelper(this, PersistWhenOutput.IsPresent);
            // Start processing the proxy command
            formatProxyHelper.Begin();
        }

        protected override void ProcessRecord()
        {
            // Look up the current input object
            PSObject inputObject = null;
            if (MyInvocation.BoundParameters.ContainsKey("InputObject") &&
                (MyInvocation.BoundParameters["InputObject"] != null))
            {
                inputObject = MyInvocation.BoundParameters["InputObject"] as PSObject;
            }

            // Process any input that was received
            formatProxyHelper.Process(inputObject);
        }

        protected override void EndProcessing()
        {
            // End the processing of the proxy command
            formatProxyHelper.End();
        }
    }
}
