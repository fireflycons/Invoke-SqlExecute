#
# Module manifest for module 'ThreadJob'
#

@{

# Script module or binary module file associated with this manifest.
RootModule = '.\Firefly.InvokeSqlExecute.dll'

# Version number of this module.
ModuleVersion = '1.0'

# ID used to uniquely identify this module
GUID = 'BAD1E645-41D2-4EFB-AEFC-C541C1746C5D'

# Author of this module
Author = 'Alistair Mackay'

# Description of the functionality provided by this module
Description = "
Drop-in replacement for Invoke-Sqlcmd with the following enhancements
- Vastly improved error reporting.
- Accepts all :COMMANDs 
- 
"

# Minimum version of the Windows PowerShell engine required by this module
PowerShellVersion = '3.0'

# Cmdlets to export from this module
CmdletsToExport = 'Invoke-SqlExecute'

}
