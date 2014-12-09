## FormatPx

### Overview

FormatPx separates the formatting layer from the data layer in PowerShell. By
default, PowerShell's native Format-* cmdlets convert data objects into format
objects when are then rendered in the console. This reduces the usefulness of
the Format-* cmdlets, making it harder to work with formatting in PowerShell.
FormatPx fixes this problem by attaching format data to objects rather than
replacing objects with format data. This allows for data processing to
continue beyond Format-* cmdlets, without losing any of the capabilities of
the formatting engine in PowerShell.

### Minimum requirements

- PowerShell 3.0
- SnippetPx module

### License and Copyright

Copyright 2014 Kirk Munro

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

### Installing the FormatPx module

FormatPx is dependent on the SnippetPx module. You can download and install the
latest versions of FormatPx and SnippetPx using any of the following methods:

#### PowerShellGet

If you don't know what PowerShellGet is, it's the way of the future for PowerShell
package management. If you're curious to find out more, you should read this:
<a href="http://blogs.msdn.com/b/mvpawardprogram/archive/2014/10/06/package-management-for-powershell-modules-with-powershellget.aspx" target="_blank">Package Management for PowerShell Modules with PowerShellGet</a>

Note that these commands require that you have the PowerShellGet module installed
on the system where they are invoked.

```powershell
# If you don’t have FormatPx installed already and you want to install it for all
# all users (recommended, requires elevation)
Install-Module FormatPx,SnippetPx

# If you don't have FormatPx installed already and you want to install it for the
# current user only
Install-Module FormatPx,SnippetPx -Scope CurrentUser

# If you have FormatPx installed and you want to update it
Update-Module
```

#### PowerShell 3.0 or Later

To install from PowerShell 3.0 or later, open a native PowerShell console (not ISE,
unless you want it to take longer), and invoke one of the following commands:

```powershell
# If you want to install FormatPx for all users or update a version already installed
# (recommended, requires elevation for new install for all users)
& ([scriptblock]::Create((iwr -uri http://tinyurl.com/Install-GitHubHostedModule).Content)) -ModuleName FormatPx,SnippetPx

# If you want to install FormatPx for the current user
& ([scriptblock]::Create((iwr -uri http://tinyurl.com/Install-GitHubHostedModule).Content)) -ModuleName FormatPx,SnippetPx -Scope CurrentUser
```

### Loading the FormatPx module

When it comes to module auto-loading, FormatPx does not function the same way
that other modules do, because the proxy commands it includes are part of core
PowerShell modules that take priority during command lookup. As a result, you
should manually load FormatPx in order to take advantage of the improvements
it makes to the PowerShell formatting engine by invoking the following command:

```powershell
Import-Module FormatPx
```

If you use FormatPx on a regular basis, you can add that to your profile
script so that it is automatically loaded in every session.

Note that if you also use the HistoryPx module, FormatPx should be imported
before HistoryPx so that HistoryPx works directly with object data instead
of format data both in the extended history table and in the automatic
output capture feature.

### Using the FormatPx module

The FormatPx module is designed to work transparently within PowerShell. Once
the module is loaded in PowerShell, you can continue using Format-Table,
Format-List, Format-Wide and Format-Custom as normally would have before. What
you can also do though that you could not do before is pipe the objects that
were passed into one of the core Format-* cmdlets into other cmdlets after the
Format-* cmdlet. For example, consider the following command and its output:

```
PS C:\> Get-Service c* | Format-Table Name,Status -AutoSize

Name              Status
----              ------
c2wts            Stopped
CertPropSvc      Running
COMSysApp        Stopped
cphs             Stopped
CrmSqlStartupSvc Running
CryptSvc         Running
CscService       Running
```

Normally after running such a command, you wouldn't be able to pipe any further
because by default, Format-Table converts object data into format data. FormatPx
changes that, such that the format data is added to the object data instead. This
allows you to then take further action beyond a call to a core Format-* cmdlet,
like this:

```powershell
PS C:\> Get-Service c* | Format-Table Name,Status -AutoSize | Stop-Service -WhatIf
What if: Performing the operation "Stop-Service" on target "Claims to Windows Token Service (c2wts)".
What if: Performing the operation "Stop-Service" on target "Certificate Propagation (CertPropSvc)".
What if: Performing the operation "Stop-Service" on target "COM+ System Application (COMSysApp)".
What if: Performing the operation "Stop-Service" on target "Intel(R) Content Protection HECI Service (cphs)".
What if: Performing the operation "Stop-Service" on target "SQL Server (CRM) On-Demand Shutdown (CrmSqlStartupSvc)".
What if: Performing the operation "Stop-Service" on target "Cryptographic Services (CryptSvc)".
What if: Performing the operation "Stop-Service" on target "Offline Files (CscService)".
```

This separation of the formatting layer from the data processing layer in
PowerShell allows you to use Format-Table, Format-List, Format-Wide, or
Format-Custom inside of a function to define a rich format layout for the data
returned by your function without compromising the ability to work with the
object data in PowerShell. For example, consider this function:

```
PS C:\> function Get-SystemInfo {
    [CmdletBinding()]
    [OutputType('System.Info')]
    param()
    $computerSystem = Get-CimInstance Win32_ComputerSystem
    $operatingSystem = Get-CimInstance Win32_OperatingSystem
    $systemInfo = [pscustomobject]@{
        PSTypeName = 'System.Info'
        ComputerName = $computerSystem.Name
        DomainName = $computerSystem.Domain
        Manufacturer = $computerSystem.Manufacturer
        Model = $computerSystem.Model
        NumProcessors = $computerSystem.NumberOfProcessors
        OperatingSystem = $operatingSystem.Caption 
    }
    $systemInfo | Format-Table ComputerName,OperatingSystem -AutoSize -PersistWhenOutput
}
```

If you run that function, you will get back a custom object of type System.Info
along with a well defined default format for that custom object type. This sort
of thing would have required using complicated format ps1xml files in the past,
but now you can apply custom formatting much easier by simply using Format-*
cmdlets!

In addition to the separation of format data from object data, FormatPx also
modifies the default PowerShell behaviour to prevent certain types of objects
from being displayed in the format that you desire without using the -Force
parameter. For example, consider this snippet:

```
PS C:\> $sb = {'Script block'}
PS C:\> $sb | Format-List
```

Without FormatPx, you would simply see this displayed to your console:

```
'ScriptBlock'
```

With FormatPx, however, you now see the format that you asked for, without having
to be redundant about how you request it by using -Force. It would look like this:

```
Attributes      : {}
File            :
IsFilter        : False
IsConfiguration : False
Module          :
StartPosition   : System.Management.Automation.PSToken
DebuggerHidden  : False
Ast             : {'ScriptBlock'}
```

This trick also works for errors in the $error variable, exceptions in the Exception
property on ErrorRecord objects, and other object types that are configured by
default to hide details that really are important enough to be able to see without
using -Force.

There are a few technical details that you should know about if you want to give
this module a spin in your environment.

First, when you are defining a default format, whether it be a table, list, wide
table, or a custom format, you should use the new -PersistWhenOutput switch
parameter. That parameter instructs PowerShell to keep the format data attached
to the objects that are output even after the format data has been rendered in
the current console. By default, format data is removed after it is rendered to
the console, allowing you to continue using Format-* cmdlets as you would have
in the past.

Second, you can define (or override) the default format for any object by simply
passing it to the format cmdlet of your choice and applying the -PersistWhenOutput
switch.

Third, since format data is now simply added to an object, you can apply multiple
format cmdlets to a single set of data in one pipeline. You would likely only do
this if the format defined inside of a function was not what you wanted, but you
can mix and match format calls however you choose. For example:

```
PS C:\> Get-Service c* | ft | fw | fc | fl
```

Lastly, there is a new format cmdlet in this module called Format-Default. The
Format-Default cmdlet allows you to take anything that has format data associated
with it and instruct PowerShell to render it using the default format instead,
whatever that may be. You can also invoke Format-Default using the convenient fd
alias as well.

That should give you a good idea of what is included in this module. If you have
ideas on what else you might like to see related to formatting in PowerShell, please
let me know on the GitHub page.