using System;
using System.Linq;
using System.Management.Automation;

namespace FormatPx
{
    internal class ProxyCmdletHelper
    {
        public ProxyCmdletHelper(PSCmdlet proxyCmdlet)
        {
            this.proxyCmdlet = proxyCmdlet;
        }

        protected PSCmdlet proxyCmdlet = null;
        protected SteppablePipeline steppablePipeline = null;

        public void Begin(bool acceptsPipelineInput)
        {
            // Find the first matching cmdlet after the proxy cmdlet in the prioritized cmdlet order
            CmdletInfo proxyTarget = proxyCmdlet.InvokeCommand.GetCmdlets(proxyCmdlet.MyInvocation.MyCommand.Name)
                .SkipWhile(x => x.ImplementingType != proxyCmdlet.GetType())
                .First(x => x.ImplementingType != proxyCmdlet.GetType());

            // Define the steppable pipeline that we want to do the work (make sure you always
            // add the full command name; otherwise, you risk proxying the wrong command or
            // getting into an endless loop).
            PowerShell ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.AddCommand(string.Format(@"{0}\{1}", proxyTarget.ModuleName, proxyTarget.Name), false);
            foreach (string parameterName in proxyCmdlet.MyInvocation.BoundParameters.Keys)
            {
                ps.AddParameter(parameterName, proxyCmdlet.MyInvocation.BoundParameters[parameterName]);
            }

            // Invoke the steppable pipeline
            steppablePipeline = ps.GetSteppablePipeline(proxyCmdlet);
            steppablePipeline.Begin(acceptsPipelineInput);
        }

        public void ProcessInputObject()
        {
            // Look up the current input object
            PSObject inputObject = null;
            if (proxyCmdlet.MyInvocation.BoundParameters.ContainsKey("InputObject") &&
                (proxyCmdlet.MyInvocation.BoundParameters["InputObject"] != null))
            {
                inputObject = proxyCmdlet.MyInvocation.BoundParameters["InputObject"] as PSObject;
            }
            
            // Now process the input object
            Process(inputObject);
        }

        public virtual void Process(PSObject inputObject)
        {
            // If there is no steppable pipeline, return immediately
            if (steppablePipeline == null)
            {
                return;
            }

            // Process the steppable pipeline
            foreach (PSObject item in (inputObject == null ? steppablePipeline.Process() : steppablePipeline.Process(inputObject)))
            {
                proxyCmdlet.WriteObject(item);
            }
        }

        public virtual void End()
        {
            // End the processing of the steppable pipeline
            if (steppablePipeline != null)
            {
                foreach (PSObject item in steppablePipeline.End())
                {
                    proxyCmdlet.WriteObject(item);
                }
            }
        }
    }
}
