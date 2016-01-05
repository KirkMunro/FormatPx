using System.Collections.Generic;
using System.Management.Automation;

namespace FormatPx
{
    [Cmdlet(
        VerbsCommon.Format,
        "Default"
    )]
    [OutputType(typeof(object))]
    public class FormatDefaultCommand : PSCmdlet
    {
        [Parameter(
            ValueFromPipeline = true,
            HelpMessage = "Specifies the objects to be formatted. Enter a variable that contains the objects, or type a command or expression that gets the objects."
        )]
        public PSObject InputObject = null;

        [Parameter(
            HelpMessage = "Persists the format data on the object when it is output. By default, format data is discarded when output."
        )]
        [Alias("Sticky")]
        public SwitchParameter PersistWhenOutput = false;

        ViewFinder viewFinder = null;

        Dictionary<string, object> initialParameters = null;
        SteppablePipeline formatPipeline = null;
        PowerShellInternals.DefaultViewHelper currentView = null;

        protected override void BeginProcessing()
        {
            // Create a new view finder
            viewFinder = new ViewFinder(this);

            // Capture the input parameters (these are used for late-opening of
            // the steppable pipeline)
            initialParameters = new Dictionary<string, object>(MyInvocation.BoundParameters);

            // Let the base class do its work
            base.BeginProcessing();
        }

        protected void CloseDefaultFormatPipeline()
        {
            // If we were using a default format pipeline, allow it to finish and close it
            if (formatPipeline != null)
            {
                foreach (PSObject item in formatPipeline.End())
                {
                    WriteObject(item);
                }
            }

            currentView = null;
            formatPipeline = null;
        }

        protected override void ProcessRecord()
        {
            // If no input was received, do nothing
            if (InputObject == null)
            {
                return;
            }

            // Lookup the default format for the current object type
            PowerShellInternals.DefaultViewHelper defaultView = viewFinder.GetDefaultView(InputObject);

            // If the default view control is different, close the current steppable pipeline
            if (currentView != null && !currentView.Equals(defaultView))
            {
                CloseDefaultFormatPipeline();
            }

            // If the default view control is not set yet, set it and open a steppable pipeline
            // if the object is not a scalar
            if (currentView == null)
            {
                currentView = defaultView;
                string currentViewType = currentView.GetViewType();
                if (string.Compare(currentViewType, "Scalar") != 0)
                {
                    PowerShell ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
                    string defaultFormatCommand = string.Format("Format-{0}", currentViewType);
                    ps.AddCommand(string.Format(@"FormatPx\{0}", defaultFormatCommand), false);
                    foreach (string parameterName in initialParameters.Keys)
                    {
                        ps.AddParameter(parameterName, initialParameters[parameterName]);
                    }
                    formatPipeline = ps.GetSteppablePipeline(this);
                    formatPipeline.Begin(!initialParameters.ContainsKey("InputObject"));
                }
            }

            // If the default view control is the same, send the object into the steppable pipeline,
            // unless it is a scalar
            if (currentView != null && currentView.Equals(defaultView))
            {
                if (formatPipeline != null)
                {
                    foreach (PSObject item in (initialParameters.ContainsKey("InputObject") ? formatPipeline.Process() : formatPipeline.Process(InputObject)))
                    {
                        WriteObject(item);
                    }
                }
                else
                {
                    InputObject.Properties.Remove("__FormatData");
                    WriteObject(InputObject);
                }
            }

            // Let the base class do its work
            base.ProcessRecord();
        }

        protected override void EndProcessing()
        {
            // Close the helper steppable pipeline if we have one open
            CloseDefaultFormatPipeline();

            // Let the base class do its work
            base.EndProcessing();
        }
    }
}