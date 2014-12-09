using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace FormatPx
{
    internal class FormatProxyCmdletHelper : ProxyCmdletHelper
    {
        internal static Type FormatStartData = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.FormatStartData, System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type GroupStartData  = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.GroupStartData,  System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type FormatEntryData = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData, System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type GroupEndData    = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.GroupEndData,    System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type FormatEndData   = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.FormatEndData,   System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

        bool persistWhenOutput = false;
        dynamic format = null;
        dynamic group = null;
        Collection<object> outOfBandFormatData = new Collection<object>();

        public FormatProxyCmdletHelper(PSCmdlet proxyCmdlet, bool persistWhenOutput)
            : base(proxyCmdlet)
        {
            this.persistWhenOutput = persistWhenOutput;
        }

        public void Begin()
        {
            base.Begin(!proxyCmdlet.MyInvocation.BoundParameters.ContainsKey("InputObject"));
        }

        public override void Process(PSObject inputObject)
        {
            // If we don't have a stappable pipeline, return immediately
            if (steppablePipeline == null)
            {
                return;
            }

            // If we don't have any input, return immediately (this differs from the base class
            // and from the proxy target, however since we're attaching format data to objects,
            // and since null is not an object, we need to throw nulls away)
            if (inputObject == null)
            {
                return;
            }

            // Create a format entry placeholder
            dynamic entry = null;
            // Create a collection for any format entry data that is related to this input object
            Collection<FormatEntry> entryCollection = new Collection<FormatEntry>();
            // Create a collection for any out of band format data that is related to this input object
            Collection<object> outOfBandFormatData = new Collection<object>();
            // Process the proxy target results
            foreach (PSObject item in steppablePipeline.Process(inputObject))
            {
                if ((item.Properties["outOfBand"] != null) &&
                    ((bool)item.Properties["outOfBand"].Value))
                {
                    // When out of band entries are received, track them until we can determine what
                    // to do with them
                    outOfBandFormatData.Add(item.BaseObject);
                }
                else if (FormatEntryData.IsInstanceOfType(item.BaseObject))
                {
                    // Add any out of band data to the current entry
                    AddOobInfoToFormatRecord(entry, ref outOfBandFormatData);
                    // Set the current entry's format data
                    entry = new FormatEntry();
                    entry.Data = item.BaseObject;
                    entryCollection.Add(entry);

                }
                else if (GroupStartData.IsInstanceOfType(item.BaseObject))
                {
                    // Create the group object and set the start value
                    group = new FormatContainer();
                    group.Start = item.BaseObject;
                }
                else if (GroupEndData.IsInstanceOfType(item.BaseObject))
                {
                    // Reset the entry placeholder
                    entry = null;
                    // Add any out of band data to the current entry
                    AddOobInfoToFormatRecord(entry, ref outOfBandFormatData);
                    // Add the end value to the current group object
                    group.End = item.BaseObject;
                }
                else if (FormatStartData.IsInstanceOfType(item.BaseObject))
                {
                    // Create the format object and set the start value
                    format = new FormatContainer();
                    format.Start = item.BaseObject;
                    // Add any out of band data to the format container
                    AddOobInfoToFormatRecord(format, ref outOfBandFormatData);
                }
                else
                {
                    // This should never happen
                    throw new ArgumentException(string.Format("An object of an unexpected type ({0}) was received while processing format data.", item.BaseObject.GetType().FullName));
                }
            }

            // Add any out of band data to the current entry
            AddOobInfoToFormatRecord(entry, ref outOfBandFormatData);

            // Create the object representing the data record
            dynamic record = new FormatRecord(format, group, entryCollection, persistWhenOutput);
            record.Format = format;
            record.Group = group;
            record.Entry = entryCollection;

            // Add the format record to the input object
            AddFormatRecordToPSObject(inputObject, record);

            // Once the formatting is finished for the object, write it to the pipeline
            proxyCmdlet.WriteObject(inputObject);
        }

        private static void AddOobInfoToFormatRecord(dynamic entry, ref Collection<object> outOfBandFormatData)
        {
            // If we don't have an entry, return immediately
            if (entry == null)
            {
                return;
            }

            // If we don't have out of band format information, return immediately
            if (outOfBandFormatData.Count == 0)
            {
                return;
            }

            // If we have out of band format information and we're finishing the processing
            // of an object, add it to the object before writing it to the pipeline
            entry.OobData = outOfBandFormatData;
            outOfBandFormatData = new Collection<object>();
        }

        internal static void AddFormatRecordToPSObject(PSObject inputObject, dynamic record)
        {
            // Attach the collection of format objects that is emitted from the pipeline
            // to the object that was input using a well-known ETS property name, being
            // mindful of how persistence should work
            if (inputObject.Properties["__FormatData"] != null)
            {
                Stack<FormatRecord> currentFormatDataStack = (Stack<FormatRecord>)inputObject.Properties["__FormatData"].Value;
                // Pop off any non-persistent format data that is on the stack already
                while ((currentFormatDataStack.Count > 0) &&
                        !currentFormatDataStack.Peek().PersistWhenOutput)
                {
                    currentFormatDataStack.Pop();
                }
                // If the current item and the top item on the stack are both persistent,
                // pop the top item off the stack since we're replacing it
                if (record.PersistWhenOutput &&
                    (currentFormatDataStack.Count > 0) &&
                    currentFormatDataStack.Peek().PersistWhenOutput)
                {
                    currentFormatDataStack.Pop();
                }
                if (record.UseDefault && (currentFormatDataStack.Count == 0))
                {
                    // When changing to default output with an empty format data stack, simply
                    // clear the format data from the object instead and let PowerShell do the
                    // rest
                    inputObject.Properties.Remove("__FormatData");
                }
                else
                {
                    // Otherwise push the new format data on the stack and assign it back to
                    // inputObject
                    currentFormatDataStack.Push(record);
                    inputObject.Properties["__FormatData"].Value = currentFormatDataStack;
                }
            }
            else if (!record.UseDefault)
            {
                // Put the object representing the data record on a stack so that we can
                // persist format properly
                Stack<FormatRecord> formatDataStack = new Stack<FormatRecord>();
                formatDataStack.Push(record);
                // Now add the stack to a the input object
                inputObject.Properties.Add(new PSNoteProperty("__FormatData", formatDataStack));
            }
        }

        public override void End()
        {
            // End the processing of the steppable pipeline
            if (steppablePipeline != null)
            {
                foreach (PSObject item in steppablePipeline.End())
                {
                    if (GroupEndData.IsInstanceOfType(item.BaseObject))
                    {
                        // Add the end value to the current group object
                        group.End = item.BaseObject;
                    }
                    else if (FormatEndData.IsInstanceOfType(item.BaseObject))
                    {
                        // Add the end value to the current format object
                        format.End = item.BaseObject;
                    }
                    else
                    {
                        // This should never happen
                        throw new ArgumentException(string.Format("An object of an unexpected type ({0}) was received while completing the processing of format data.", item.BaseObject.GetType().FullName));
                    }
                }
            }
        }
    }
}
