# Pester tests for the module

# Dot-source helpers
. "$PSScriptRoot\TestHelpers.ps1"

$ModuleName = 'Firefly.InvokeSqlExecute'

# Check we are running in AppVeyor
if (${ENV:BHBuildSystem} -ne 'AppVeyor')
{
    Write-Host "AppVeyor not detected. Skipping tests in $(Split-Path -Leaf $MyInvocation.MyCommand.Definition)"
}

# Enumerate available SQL instances
Write-Host 'Detecting SQL Server instances...'

$instances = Get-ChildItem -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\' |
    Where-Object {
    $_.Name -imatch 'MSSQL[_\d]+\.SQL.*'
} |
    ForEach-Object {

    $instance = (Get-ItemProperty $_.PSPath).'(default)'
    Write-Host -NoNewline "- Found $instance. Getting details... "
    Get-SqlServerInstanceData -InstanceName $instance -ConnectionString "Server=(local)\$instance;User ID=sa;Password=Password12!"
}

# Enumerate localdb instances
$instances += 'v11.0','MSSQLLocalDB' |
ForEach-Object {

    $instance = "(localdb)\$_"
    Write-Host "Checking for $instance ... "
    Get-SqlServerInstanceData -InstanceName $instance -ConnectionString "Server=$instance;Integrated Security=true";
}

if (($instances | Measure-Object).Count -eq 0)
{
    Write-Warning "No SQL Server instances found"
}
elseif ($instances | Where-Object { $_.IsFullTextInstalled} )
{
    $awDirs = Get-AdventureWorksClone
}
else
{
    Write-Warning "None of the SQL instances support Full Text. AdventureWorks tests will be inconclusive."
}


# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$ManifestFile = "$(Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition))\$ModuleName\$ModuleName.psd1"

# Import the module
Import-Module -Name $ManifestFile

Describe 'AdventureWorks Database Creation' {

    $instances |
    Foreach-Object {

        $instanceInfo = $_

        Context $instanceInfo.Instance {

            It 'Creates AdventureWorks OLTP Database' {

                if ($instanceInfo.IsFullTextInstalled)
                {
                    {
                        Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $awDirs.OltpDir 'instawdb.sql') -Variable @{ SqlSamplesSourceDataPath = "$($awDirs.OltpDir)\" } -OverrideScriptVariables
                    } |
                    Should Not Throw
                }
                else 
                {
                    Set-TestInconclusive -Message "Full Text Indexing not supported on this instance"   
                }
            }

            It 'Creates AdventureWorks Data Warehouse' {

                if ($instanceInfo.IsFullTextInstalled)
                {
                    {
                        Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $awDirs.DwDir 'instawdbdw.sql') -Variable @{ SqlSamplesSourceDataPath = "$($awDirs.DwDir)\" } -OverrideScriptVariables
                    } |
                    Should Not Throw
                }
                else 
                {
                    Set-TestInconclusive -Message "Full Text Indexing not supported on this instance"   
                }
            }
        }
    }
}
