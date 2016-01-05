using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("FormatPx")]
[assembly: AssemblyDescription("FormatPx separates the formatting layer from the data layer in PowerShell. By default, PowerShell's native Format-* cmdlets convert data objects into format objects when are then rendered in the console. This reduces the usefulness of the Format-* cmdlets, making it harder to work with formatting in PowerShell. FormatPx fixes this problem by attaching format data to objects rather than replacing objects with format data. This allows for data processing to continue beyond Format-* cmdlets, without losing any of the capabilities of the formatting engine in PowerShell. FormatPx also removes formatting limitations in the output layer, allowing multiple contiguous formats returned by a single command to render properly in PowerShell.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Kirk Munro")]
[assembly: AssemblyProduct("FormatPx")]
[assembly: AssemblyCopyright("Copyright © 2016 Kirk Munro")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("bbfe23fd-585d-4d51-a4e2-65b87acf03e0")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.1.3.14")]
[assembly: AssemblyFileVersion("1.1.3.14")]