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

@{
      ModuleToProcess = 'FormatPx.psm1'

        ModuleVersion = '1.1.3.15'

                 GUID = 'caba4410-d4b8-4f84-bb28-4391ed908cc2'

               Author = 'Kirk Munro'

          CompanyName = 'Poshoholic Studios'

            Copyright = 'Copyright 2016 Kirk Munro'

          Description = 'FormatPx separates the formatting layer from the data processing layer in PowerShell. By default, PowerShell''s native Format-* cmdlets convert data objects into format objects when are then rendered in the console. This reduces the usefulness of the Format-* cmdlets, making it harder to work with formatting in PowerShell. FormatPx fixes this problem by attaching format data to objects rather than replacing objects with format data. This allows for data processing to continue beyond Format-* cmdlets, without losing any of the capabilities of the formatting engine in PowerShell. FormatPx also removes formatting limitations in the output layer, allowing multiple contiguous formats returned by a single command to render properly in PowerShell.'

    PowerShellVersion = '3.0'

      RequiredModules = @(
                        'Microsoft.PowerShell.Utility'
                        )

        NestedModules = @(
                        'FormatPx.dll'
                        'SnippetPx'
                        )

      CmdletsToExport = @(
                        'Format-Custom'
                        'Format-Default'
                        'Format-List'
                        'Format-Table'
                        'Format-Wide'
                        'Out-Default'
                        'Out-File'
                        'Out-Host'
                        'Out-Printer'
                        'Out-String'
                        )

      AliasesToExport = @(
                        'fd'
                        )

             FileList = @(
                        'FormatPx.dll'
                        'FormatPx.psd1'
                        'FormatPx.psm1'
                        'LICENSE'
                        'NOTICE'
                        'en-us\FormatPx.dll-help.xml'
                        'scripts\Export-BinaryModule.ps1'
                        )

          PrivateData = @{
                            PSData = @{
                                ExternalModuleDependencies = @(
                                    'Microsoft.PowerShell.Utility'
                                )
                                Tags = 'format Format-Table Format-List Format-Wide Format-Custom Format-Default Out-Default Out-File Out-Host Out-Printer Out-String'
                                LicenseUri = 'http://apache.org/licenses/LICENSE-2.0.txt'
                                ProjectUri = 'https://github.com/KirkMunro/FormatPx'
                                IconUri = ''
                                ReleaseNotes = 'This module will not automatically load by invoking a Format-* command because the native, core Format-* cmdlets are loaded first in PowerShell. To start using FormatPx, you should explicitly import the module either at the command line or as part of your profile by invoking "Import-Module FormatPx".'
                           }
                        }
}