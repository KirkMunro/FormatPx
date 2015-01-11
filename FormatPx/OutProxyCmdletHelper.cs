using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace FormatPx
{
    internal class OutProxyCmdletHelper : ProxyCmdletHelper
    {
        dynamic format = null;
        dynamic group = null;

        public OutProxyCmdletHelper(PSCmdlet proxyCmdlet)
            : base(proxyCmdlet)
        {
        }

        public void Begin()
        {
            // Reset the format helper variables
            format = null;
            group = null;

            // Let the base class do its work
            base.Begin(!proxyCmdlet.MyInvocation.BoundParameters.ContainsKey("InputObject"));
        }

        protected void ProcessObject(object obj)
        {
            // Skip null objects automatically
            if (obj == null)
            {
                return;
            }

            // Let the proxy target do its work
            base.Process(obj is PSObject ? obj as PSObject : new PSObject(obj));
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

        public override void Process(PSObject inputObject)
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
                try
                {
                    // Get the topmost record on the stack, persisting the stack if the
                    // PersistWhenOutput property is set
                    Stack<FormatRecord> formatRecordStack = (Stack<FormatRecord>)formatDataProperty.Value;
                    FormatRecord record = formatRecordStack.Peek().PersistWhenOutput ? formatRecordStack.Peek() : formatRecordStack.Pop();
                    // If the stack is empty, remove the format data from the object
                    if (formatRecordStack.Count == 0)
                    {
                        inputObject.Properties.Remove("__FormatData");
                    }
                    // Determine whether or not we're working with the default format
                    if (record.UseDefault)
                    {
                        // If we're outputting default format data, make sure we close any
                        // current group/format we are outputting and then send the input
                        // object to the pipeline
                        CloseFormatContainers();
                        ProcessObject(inputObject);
                    }
                    else
                    {
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
                }
                catch
                {
                    // Close any format containers that we currently have open
                    CloseFormatContainers();
                    throw;
                }
            }
            else
            {
                // Let the base class do its work
                base.Process(inputObject);
            }
        }

        public override void End()
        {
            // Close any format containers that we currently have open
            CloseFormatContainers();

            // Let the base class do its work
            base.End();
        }
    }
}
