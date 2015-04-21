using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Linq;
using System.Text.RegularExpressions;

namespace FormatPx
{
    internal class OutProxyCmdletHelper : ProxyCmdletHelper
    {
        internal static Type FormatStartData = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.FormatStartData, System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type GroupStartData  = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.GroupStartData,  System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type FormatEntryData = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData, System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type GroupEndData    = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.GroupEndData,    System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type FormatEndData   = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.FormatEndData,   System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

        private static string writeErrorStream = "writeErrorStream";
        private static string writeWarningStream = "writeWarningStream";
        private static string writeVerboseStream = "writeVerboseStream";
        private static string writeDebugStream = "writeDebugStream";
        private static string writeProgressStream = "writeProgressStream";
        private static string writeInformationStream = "writeInformationStream";

        dynamic format = null;
        dynamic group = null;

        Dictionary<string, PowerShellInternals.DefaultViewHelper> defaultViewMap = new Dictionary<string, PowerShellInternals.DefaultViewHelper>(StringComparer.OrdinalIgnoreCase);

        SteppablePipeline formatPipeline = null;
        PowerShellInternals.DefaultViewHelper currentView = null;
        PowerShellInternals.DisplayDataQueryHelper displayDataQueryHelper;

        public OutProxyCmdletHelper(PSCmdlet proxyCmdlet)
            : base(proxyCmdlet)
        {
        }

        public override void Begin()
        {
            // Reset the format helper variables
            format = null;
            group = null;

            // Create an instance of the DisplayDataQueryHelper class
            displayDataQueryHelper = proxyCmdlet.GetDisplayDataQueryHelper();

            // Let the base class do its work
            base.Begin();
        }

        protected void ProcessObject(object obj)
        {
            // Skip null objects automatically
            if (obj == null)
            {
                return;
            }

            // Let the proxy target do its work, expanding collections automatically
            PSObject psObject = obj is PSObject ? obj as PSObject : new PSObject(obj);
            if (psObject.BaseObject is IEnumerable)
            {
                foreach (object nestedObject in (psObject.BaseObject as IEnumerable))
                {
                    PSObject nestedPsObject = nestedObject is PSObject ? nestedObject as PSObject : new PSObject(nestedObject);
                    base.Process(nestedPsObject);
                }
            }
            else
            {
                base.Process(psObject);
            }
        }

        protected void ProcessOobData(Collection<object> oobData)
        {
            // Skip null OobData collections automatically
            if (oobData == null)
            {
                return;
            }

            // Process the objects in the OobData collection
            foreach (var oobRecord in oobData)
            {
                ProcessObject(oobRecord);
            }
        }

        protected void CloseFormatContainers()
        {
            // If we were processing format data, be sure to send out the group and format
            // end objects once we're all done to close the format collections properly
            if (group != null)
            {
                ProcessObject(group.End);
                group = null;
            }
            if (format != null)
            {
                ProcessObject(format.End);
                format = null;
            }
        }

        protected void CloseDefaultFormatPipeline()
        {
            // If we were using a default format pipeline, allow it to finish and close it
            if (formatPipeline != null)
            {
                foreach (PSObject item in formatPipeline.End())
                {
                    Process(item, true);
                }
            }

            currentView = null;
            formatPipeline = null;

            // Close any format containers that we currently have open
            CloseFormatContainers();

        }

        public override void Process(PSObject inputObject)
        {
            // Otherwise, process it using our custom process method
            Process(inputObject, false);
        }

        protected void Process(PSObject inputObject, bool recursiveInvocation = false)
        {
            // If the inputObject contains any format data, then process the format data
            // instead of the input object (this is key to allowing the presentation layer
            // to be properly separated from the data layer, even when using Format-*
            // commands in scripts)
            PSPropertyInfo formatDataProperty = null;
            if (inputObject != null)
            {
                formatDataProperty = inputObject.GetPropertySafe("__FormatData");
            }
            if ((formatDataProperty != null) && (formatDataProperty.Value != null))
            {
                ProcessFormattedObject(inputObject, formatDataProperty, recursiveInvocation);
            }
            else if ((inputObject.Properties[writeErrorStream] != null) ||
                     (inputObject.Properties[writeWarningStream] != null) ||
                     (inputObject.Properties[writeVerboseStream] != null) ||
                     (inputObject.Properties[writeDebugStream] != null) ||
                     (inputObject.Properties[writeProgressStream] != null) ||
                     (inputObject.Properties[writeInformationStream] != null) ||
                     FormatStartData.IsInstanceOfType(inputObject.BaseObject) ||
                     GroupStartData.IsInstanceOfType(inputObject.BaseObject) ||
                     FormatEntryData.IsInstanceOfType(inputObject.BaseObject) ||
                     GroupEndData.IsInstanceOfType(inputObject.BaseObject) ||
                     FormatEndData.IsInstanceOfType(inputObject.BaseObject))
            {
                base.Process(inputObject);
            }
            else if (inputObject != null)
            {
                ProcessUnformattedObject(inputObject);
            }
        }

        private void ProcessFormattedObject(PSObject inputObject, PSPropertyInfo formatDataProperty, bool recursiveInvocation)
        {
            try
            {
                // When this command is invoked directly (not recursively), the default
                // format pipeline must be closed in order to properly wrap up any of
                // the format objects that were previously output to the console
                if ((formatPipeline != null) && !recursiveInvocation)
                {
                    CloseDefaultFormatPipeline();
                }

                // Get the topmost record on the stack, persisting the stack if the
                // PersistWhenOutput property is set
                Stack<FormatRecord> formatRecordStack = (Stack<FormatRecord>)formatDataProperty.Value;
                FormatRecord record = formatRecordStack.Peek().PersistWhenOutput ? formatRecordStack.Peek() : formatRecordStack.Pop();
                // If the stack is empty, remove the format data from the object
                if (formatRecordStack.Count == 0)
                {
                    inputObject.Properties.Remove("__FormatData");
                }
                // Otherwise, process the format data
                if (record.Group != null)
                {
                    if ((group != null) && (group != record.Group))
                    {
                        // If we have a new group, process the current group's end object
                        ProcessObject(group.End);
                        group = null;
                    }
                }
                if (record.Format != null)
                {
                    if ((format != null) && (format != record.Format))
                    {
                        // If we have a new format, process the current format's end object
                        ProcessObject(format.End);
                        format = null;
                    }
                    if (format == null)
                    {
                        // If we have a new format, process its start object and oob data
                        format = record.Format;
                        ProcessOobData(format.OobData);
                        ProcessObject(format.Start);
                    }
                }
                if (record.Group != null)
                {
                    if (group == null)
                    {
                        // If we have a new group, process its start object
                        group = record.Group;
                        ProcessObject(group.Start);
                    }
                }
                // Now process the data entry and any oob data associated with it
                Collection<FormatEntry> entry = record.Entry;
                foreach (dynamic item in entry)
                {
                    ProcessObject(item.Data);
                    ProcessOobData(item.OobData);
                }
            }
            catch
            {
                // Close any format containers that we currently have open
                CloseFormatContainers();
                throw;
            }
        }

        private void ProcessUnformattedObject(PSObject inputObject)
        {
            // Get the type hierarchy for the current object and convert it to a joined string
            string typeHierarchy = string.Join("|", inputObject.TypeNames.Select(x => Regex.Replace(x, @"^Deserialized\.", "")));

            // Lookup the default format for the current object type
            PowerShellInternals.DefaultViewHelper defaultView = null;
            if (defaultViewMap.ContainsKey(typeHierarchy))
            {
                defaultView = defaultViewMap[typeHierarchy];
            }
            else
            {
                defaultView = displayDataQueryHelper.GetDefaultView(inputObject);
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

                if (!inputObject.IsScalar())
                {
                    PowerShell ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
                    string currentViewType = currentView.GetViewType();
                    string defaultFormatCommand = string.Format("Format-{0}", currentViewType);
                    ps.AddCommand(string.Format(@"FormatPx\{0}", defaultFormatCommand), false);
                    formatPipeline = ps.GetSteppablePipeline(proxyCmdlet);
                    formatPipeline.Begin(true);
                }
                else
                {
                    CloseFormatContainers();
                }
            }

            // If the default view control is the same, send the object into the steppable pipeline
            if (currentView != null && currentView == defaultView)
            {
                if (!inputObject.IsScalar())
                {
                    foreach (PSObject item in formatPipeline.Process(inputObject))
                    {
                        Process(item, true);
                    }
                }
                else
                {
                    base.Process(inputObject);
                }
            }
        }

        public override void End()
        {
            // Close the helper steppable pipeline if we have one open
            CloseDefaultFormatPipeline();

            // Let the base class do its work
            base.End();
        }
    }
}