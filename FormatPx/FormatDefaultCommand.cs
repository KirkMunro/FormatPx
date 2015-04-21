using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

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

        Dictionary<string, PowerShellInternals.DefaultViewHelper> defaultViewMap = new Dictionary<string, PowerShellInternals.DefaultViewHelper>(StringComparer.OrdinalIgnoreCase);
        PowerShellInternals.DisplayDataQueryHelper displayDataQueryHelper;
        SteppablePipeline formatPipeline = null;
        PowerShellInternals.DefaultViewHelper currentView = null;

        protected override void BeginProcessing()
        {
            // Create an instance of the DisplayDataQueryHelper class
            displayDataQueryHelper = this.GetDisplayDataQueryHelper();

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

            // Get the type hierarchy for the current object and convert it to a joined string
            string typeHierarchy = string.Join("|", InputObject.TypeNames.Select(x => Regex.Replace(x, @"^Deserialized\.", "")));

            // Lookup the default format for the current object type
            PowerShellInternals.DefaultViewHelper defaultView = null;
            if (defaultViewMap.ContainsKey(typeHierarchy))
            {
                defaultView = defaultViewMap[typeHierarchy];
            }
            else
            {
                defaultView = displayDataQueryHelper.GetDefaultView(InputObject);
                defaultViewMap.Add(typeHierarchy, defaultView);
            }

            // If the default view control is different, close the current steppable pipeline
            if (currentView != null && currentView != defaultView)
            {
                CloseDefaultFormatPipeline();
            }

            // If the default view control is not set yet, set it and open a steppable pipeline
            if (currentView == null)
            {
                currentView = defaultView;
                PowerShell ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
                string currentViewType = currentView.GetViewType();
                string defaultFormatCommand = string.Format("Format-{0}", currentViewType);

                ps.AddCommand(string.Format(@"FormatPx\{0}", defaultFormatCommand), false);
                formatPipeline = ps.GetSteppablePipeline(this);
                formatPipeline.Begin(true);
            }

            // If the default view control is the same, send the object into the steppable pipeline
            if (currentView != null && currentView == defaultView)
            {
                foreach (PSObject item in formatPipeline.Process(InputObject))
                {
                    WriteObject(item);
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