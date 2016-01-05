# Life before FormatPx.
Get-Service k*
# That shows the default table format, but you can change the format.
Get-Service k* | Format-List
# The format looks good, let's wrap that in a function.
function Get-ServiceList {Get-Service k* | Format-List}
# Now we can call it.
Get-ServiceList
# But we can't do anything with it.
Get-ServiceList | Stop-Service -WhatIf
# This is because the core Format-* cmdlets convert object data into format data.
Get-ServiceList | ForEach-Object {$_.GetType().FullName} | Select-Object -Unique
# Remember those errors we got earlier? Let's have a closer look.
$error[0]
# That's interesting, but what if I want more details?
$error[0] | Format-List *
# Hmmm...I guess I wasn't explicit enough.
$error[0] | Format-List * -Force
# Ugh...can't we do better for these?
Import-Module FormatPx
# First, let's look at that error again.
$error[0] | Format-List *
# That's better, no more need to use the -Force. What about the function?
Get-ServiceList | Stop-Service -WhatIf
# Bingo! Why did that work?
Get-ServiceList | ForEach-Object {$_.GetType().FullName} | Select-Object -Unique
# The types are preserved, and the format information is attached, but hidden.
Get-ServiceList | Get-Member
# Notice how the objects don't show any additional properties by default? There is additional format information there though, it's just hidden
Get-ServiceList | Get-Member -Force
# You can see the format information by looking at the hidden __FormatData property. Now lets go even further.
$s = Get-ServiceList
# You can change the order and it still just works, but only with FormatPx.
$s[1,0]
# But now that I have output the format from a variable, I have lost it
$s
# That is necessary to protect the default format of built-in variables that haven't been formatted. To create a format that is sticky on variables, there is a new -PersistWhenOutput parameter
function Get-ServiceList {Get-Service k* | Format-List -PersistWhenOutput}
# Now I will store the results again
$s = Get-ServiceList
# And output it
$s
# And output it again
$s
# I can temporarily change the sticky format.
$s | Format-Table Name,Status -AutoSize
# And the stored format is preserved.
$s
# I can also replace the sticky format.
$s | Format-Wide -PersistWhenOutput
# Now the preserved format is different.
$s
# And I can reset and go back to default whenever I like.
$s | Format-Default -PersistWhenOutput
# This removes all stored formatting, so output is now according to whatever PowerShell considers the default.
$s
# That's FormatPx in a nutshell. What do you think?