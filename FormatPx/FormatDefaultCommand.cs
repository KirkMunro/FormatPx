using System;
using System.Collections.Generic;
using System.Linq;
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
        public SwitchParameter PersistWhenOutput = false;

        protected override void ProcessRecord()
        {
            // Look up the current input object
            PSObject inputObject = null;
            if (MyInvocation.BoundParameters.ContainsKey("InputObject") &&
                (MyInvocation.BoundParameters["InputObject"] != null))
            {
                inputObject = MyInvocation.BoundParameters["InputObject"] as PSObject;
            }

            // If no input was received, do nothing
            if (inputObject == null)
            {
                return;
            }

            // Create the object representing the data record
            dynamic record = new FormatRecord(PersistWhenOutput.IsPresent);

            // Add the format record to the input object
            FormatProxyCmdletHelper.AddFormatRecordToPSObject(inputObject, record);

            // Then write the input object to the pipeline
            WriteObject(inputObject);
        }
    }
}
