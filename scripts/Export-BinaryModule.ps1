<#############################################################################
FormatPx separates the formatting layer from the data processing layer in
PowerShell. By default, PowerShell's native Format-* cmdlets convert data
objects into format objects when are then rendered in the console. This
reduces the usefulness of the Format-* cmdlets, making it harder to work with
formatting in PowerShell. FormatPx fixes this problem by attaching format data
to objects rather than replacing objects with format data. This allows for
data processing to continue beyond Format-* cmdlets, without losing any of the
capabilities of the formatting engine in PowerShell.

Copyright 2015 Kirk Munro

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
#############################################################################>

# Export the cmdlets that are defined in the nested module
Export-ModuleMember -Cmdlet Format-Custom,Format-Default,Format-List,Format-Table,Format-Wide,Out-Default,Out-File,Out-Host,Out-Printer,Out-String

# Define a fd alias so that using Format-Default is easier.
if (-not (Get-Alias -Name fd -ErrorAction Ignore)) {
    New-Alias -Name fd -Value Format-Default
    Export-ModuleMember -Alias fd
}