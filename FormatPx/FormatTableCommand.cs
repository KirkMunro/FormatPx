using System;
using System.Management.Automation;

namespace FormatPx
{
    [Cmdlet(
        VerbsCommon.Format,
        "Table",
        HelpUri = "http://go.microsoft.com/fwlink/p/?linkid=293962"
    )]
    [OutputType(typeof(object))]
    public class FormatTableCommand : Microsoft.PowerShell.Commands.FormatTableCommand
    {
        FormatProxyCmdletHelper formatProxyHelper = null;

        [Parameter(
            HelpMessage = "Persists the format data on the object when it is output. By default, format data is discarded when output."
        )]
        [Alias("Sticky")]
        public SwitchParameter PersistWhenOutput = false;

        protected override void BeginProcessing()
        {
            // Use the proxy cmdlet helper to keep the code dry
            formatProxyHelper = new FormatProxyCmdletHelper(this);
            // Start processing the proxy command
            formatProxyHelper.Begin();
        }

        protected override void ProcessRecord()
        {
            // Process any input that was received
            formatProxyHelper.ProcessInputObject();
        }

        protected override void EndProcessing()
        {
            // End the processing of the proxy command
            formatProxyHelper.End();
        }
    }
}