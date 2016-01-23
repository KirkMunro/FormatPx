using System;
using System.Collections.Generic;
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
        protected Dictionary<string, object> initialParameters = null;
        protected SteppablePipeline steppablePipeline = null;

        public virtual void OpenSteppablePipeline()
        {
            if (steppablePipeline == null)
            {
                // Find the first matching cmdlet after the proxy cmdlet in the prioritized cmdlet order
                CmdletInfo proxyTarget = proxyCmdlet.InvokeCommand.GetCmdlets(proxyCmdlet.MyInvocation.MyCommand.Name)
                    .SkipWhile(x => x.ImplementingType != proxyCmdlet.GetType())
                    .FirstOrDefault(x => x.ImplementingType != proxyCmdlet.GetType());
                // If we can't find the command, raise an exception
                if (proxyTarget == null)
                {
                    // This can happen if someone loads the binary module directly, without the manifest (which sets up
                    // module dependencies properly -- the manifest is an absolute requirement)
                    throw new CommandNotFoundException(string.Format("Unexpected error: unable to find proxy cmdlet target with name '{0}'.", proxyCmdlet.MyInvocation.MyCommand.Name));
                }

                // Define the steppable pipeline that we want to do the work (make sure you always
                // add the full command name; otherwise, you risk proxying the wrong command or
                // getting into an endless loop).
                List<string> forcedNouns = new List<string>(new string[] { "Table", "List", "Wide" });
                PowerShell ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
                ps.AddCommand(string.Format(@"{0}\{1}", proxyTarget.ModuleName, proxyTarget.Name), false);
                bool forcedCommand = (string.Compare(proxyTarget.Verb, "Format", true) == 0) && (forcedNouns.Contains(proxyTarget.Noun, StringComparer.OrdinalIgnoreCase));
                foreach (string parameterName in initialParameters.Keys.Where(x => !forcedCommand || string.Compare(x, "Force", false) != 0))
                {
                    ps.AddParameter(parameterName, initialParameters[parameterName]);
                }

                // Add the Force parameter to Format-Table, Format-List, and Format-Wide calls
                if (forcedCommand)
                {
                    ps.AddParameter("Force", true);
                }

                // Invoke the steppable pipeline
                steppablePipeline = ps.GetSteppablePipeline(proxyCmdlet);
                steppablePipeline.Begin(!initialParameters.ContainsKey("InputObject"));
            }
        }

        public virtual void Begin()
        {
            // Capture the input parameters (these are used for late-opening of
            // the steppable pipeline)
            initialParameters = new Dictionary<string, object>(proxyCmdlet.MyInvocation.BoundParameters);
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

            // If the inputObject is null, return immediately
            if (inputObject == null)
            {
                return;
            }

            // Process the input object
            Process(inputObject);
        }

        public virtual void Process(PSObject inputObject)
        {
            // If there is no steppable pipeline, open one immediately
            OpenSteppablePipeline();

            // Process the steppable pipeline
            foreach (PSObject item in (inputObject == null ? steppablePipeline.Process() : steppablePipeline.Process(inputObject)))
            {
                proxyCmdlet.WriteObject(item);
            }
        }

        public virtual void CloseSteppablePipeline()
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

        public virtual void End()
        {
            // Close the steppable pipeline
            CloseSteppablePipeline();
        }
    }
}