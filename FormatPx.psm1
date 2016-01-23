<#############################################################################
FormatPx separates the formatting layer from the data processing layer in
PowerShell. By default, PowerShell's native Format-* cmdlets convert data
objects into format objects when are then rendered in the console. This
reduces the usefulness of the Format-* cmdlets, making it harder to work with
formatting in PowerShell. FormatPx fixes this problem by attaching format data
to objects rather than replacing objects with format data. This allows for
data processing to continue beyond Format-* cmdlets, without losing any of the
capabilities of the formatting engine in PowerShell. FormatPx also removes
formatting limitations in the output layer, allowing multiple contiguous
formats returned by a single command to render properly in PowerShell.

Copyright 2016 Kirk Munro

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

#region Set up a module scope trap statement so that terminating errors actually terminate.

trap {throw $_}

#endregion

#region Initialize the module.

Invoke-Snippet -Name Module.Initialize

#endregion

#region If HistoryPx is loaded already, raise a warning.

# When HistoryPx is loaded before FormatPx, it will receive format data objects
# from the Out-Default proxy cmdlet in FormatPx. If HistoryPx is loaded after
# FormatPx, then historical data will be captured before the formatting objects
# are extracted and sent through to Out-Default. I plan to fix this so that the
# two modules work properly regardless of their load order.

if (Get-Module -Name HistoryPx) {
    Write-Warning -Message 'The FormatPx module was loaded into a session where the HistoryPx module was already loaded. When you load these modules in this order, the smart capture variable and the Output property in the extended history table will contain format data. If you prefer ensuring that format data is only used for output (as it should be), load FormatPx before loading HistoryPx.'
}

#endregion

#region Export commands defined in nested modules.

. $PSModuleRoot\scripts\Export-BinaryModule.ps1

#endregion