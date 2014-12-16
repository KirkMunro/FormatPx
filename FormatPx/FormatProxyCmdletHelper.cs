using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Linq;
using System.Reflection;

namespace FormatPx
{
    internal class FormatProxyCmdletHelper : ProxyCmdletHelper
    {
        internal static Type FormatStartData = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.FormatStartData, System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type GroupStartData  = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.GroupStartData,  System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type FormatEntryData = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.FormatEntryData, System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type GroupEndData    = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.GroupEndData,    System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        internal static Type FormatEndData   = Type.GetType("Microsoft.PowerShell.Commands.Internal.Format.FormatEndData,   System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

        internal static string notInAGroup = "outsider_group_979d659e-59a9-4554-8499-f782debbfc2b";

        bool persistWhenOutput = false;
        dynamic format = null;
        dynamic group = null;
        Collection<object> outOfBandFormatData = new Collection<object>();
        Dictionary<string, dynamic> groups = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

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
                    // Set the default as not belonging to an official group
                    string groupValue = notInAGroup;
                    // Try to look up the group value on the object itself
                    PSObject propertyPos = item;
                    PSPropertyInfo propertyInfo = propertyPos.Properties["groupingEntry"];
                    if ((propertyInfo != null) && (propertyInfo.Value != null))
                    {
                        propertyPos = new PSObject(propertyInfo.Value);
                        propertyInfo = propertyPos.Properties["formatValueList"];
                        if ((propertyInfo != null) && (propertyInfo.Value != null))
                        {
                            MethodInfo toArrayMethod = propertyInfo.Value.GetType().GetMethod("ToArray");
                            if (toArrayMethod != null)
                            {
                                Array collection = (Array)toArrayMethod.Invoke(propertyInfo.Value, null);
                                if (collection.Length > 0)
                                {
                                    propertyPos = new PSObject(collection.GetValue(0));
                                    propertyInfo = propertyPos.Properties["formatValueList"];
                                    if ((propertyInfo != null) && (propertyInfo.Value != null))
                                    {
                                        toArrayMethod = propertyInfo.Value.GetType().GetMethod("ToArray");
                                        if (toArrayMethod != null)
                                        {
                                            collection = (Array)toArrayMethod.Invoke(propertyInfo.Value, null);
                                            if (collection.Length > 1)
                                            {
                                                propertyPos = new PSObject(collection.GetValue(1));
                                                propertyInfo = propertyPos.Properties["propertyValue"];
                                                if ((propertyInfo != null) && (propertyInfo.Value != null))
                                                {
                                                    groupValue = propertyInfo.Value as string;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // If the item is still not in a group and GroupBy was used, look harder
                    if ((string.Compare(groupValue,notInAGroup,true) == 0) && 
                        proxyCmdlet.MyInvocation.BoundParameters.ContainsKey("GroupBy"))
                    {
                        // Check to see if we already have an appropriate group created using the value that
                        // we get from the expression/property we are grouping on
                        object groupBy = proxyCmdlet.MyInvocation.BoundParameters["GroupBy"];
                        if (groupBy is string)
                        {
                            // If we're grouping on a string property name, look up the value of that property
                            propertyInfo = inputObject.Properties[groupBy as string];
                            if ((propertyInfo != null) && (propertyInfo.Value != null))
                            {
                                groupValue = propertyInfo.Value.ToString();
                            }
                        }
                        else
                        {
                            // Otherwise, identify the expression/name pair from the GroupBy parameter
                            Hashtable selectHt = new Hashtable(StringComparer.OrdinalIgnoreCase);
                            if (groupBy is ScriptBlock)
                            {
                                // For ScriptBlock parameters, the expression is the script block and the
                                // name is the script block in string format
                                ScriptBlock expression = groupBy as ScriptBlock;
                                if (expression != null)
                                {
                                    selectHt.Add("Expression", groupBy);
                                    selectHt.Add("Name", expression.ToString());
                                }
                            }
                            else if (groupBy is Hashtable)
                            {
                                // For HashTable parameters, we extract the expression and/or name from the
                                // hashtable that was passed in.
                                Hashtable ht = groupBy as Hashtable;
                                string nameKey = ht.Keys.Cast<string>().FirstOrDefault(x => ("label".StartsWith(x, StringComparison.OrdinalIgnoreCase) &&
                                                                                             x.StartsWith("l", StringComparison.OrdinalIgnoreCase)) ||
                                                                                            ("name".StartsWith(x, StringComparison.OrdinalIgnoreCase) &&
                                                                                             x.StartsWith("n", StringComparison.OrdinalIgnoreCase)));
                                string expressionKey = ht.Keys.Cast<string>().FirstOrDefault(x => "expression".StartsWith(x, StringComparison.OrdinalIgnoreCase) &&
                                                                                                   x.StartsWith("e", StringComparison.OrdinalIgnoreCase));
                                string formatStringKey = ht.Keys.Cast<string>().FirstOrDefault(x => "formatstring".StartsWith(x, StringComparison.OrdinalIgnoreCase) &&
                                                                                                     x.StartsWith("f", StringComparison.OrdinalIgnoreCase));
                                if (!string.IsNullOrEmpty(nameKey))
                                {
                                    selectHt.Add("Name", ht[nameKey] as string);
                                }
                                if (!string.IsNullOrEmpty(expressionKey))
                                {
                                    ScriptBlock expression = ht[expressionKey] as ScriptBlock;
                                    if (expression != null)
                                    {
                                        selectHt.Add("Expression", expression);
                                        if (!selectHt.ContainsKey("Name"))
                                        {
                                            selectHt.Add("Name", expression.ToString());
                                        }
                                    }
                                }
                            }
                            // Once we have the expression and name in the hashtable, use the
                            // Select-Object cmdlet to get the value of the expression
                            PowerShell ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
                            ps.AddCommand("Select-Object", false);
                            ps.AddParameter("InputObject", inputObject);
                            ps.AddParameter("Property", selectHt);
                            foreach (PSObject psObject in ps.Invoke())
                            {
                                propertyInfo = psObject.Properties[selectHt["Name"] as string];
                                if ((propertyInfo != null) && (propertyInfo.Value != null))
                                {
                                    groupValue = propertyInfo.Value.ToString();
                                }
                            }
                        }
                    }
                    // Determine what group to use based on the group value
                    if (groups.ContainsKey(groupValue))
                    {
                        // Refer to the group that we already received during this command
                        group = groups[groupValue];
                    }
                    else
                    {
                        // Create the group object, set the start value, and add it to the groups collection
                        group = new FormatContainer();
                        group.Start = item.BaseObject;
                        groups.Add(groupValue, group);
                    }
                }
                else if (GroupEndData.IsInstanceOfType(item.BaseObject))
                {
                    // Reset the entry placeholder
                    entry = null;
                    // Add any out of band data to the current entry
                    AddOobInfoToFormatRecord(entry, ref outOfBandFormatData);
                    // Add the end value to the current group object if it is not already there
                    if (group.End == null)
                    {
                        group.End = item.BaseObject;
                    }
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
                        // Add the end value to the current group object if it is not already there
                        if (group.End == null)
                        {
                            group.End = item.BaseObject;
                        }
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
