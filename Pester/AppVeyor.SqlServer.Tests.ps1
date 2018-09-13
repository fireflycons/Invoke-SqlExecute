# Pester tests for the module

# Dot-source helpers
. "$PSScriptRoot\TestHelpers.ps1"

$ModuleName = 'Firefly.InvokeSqlExecute'

# Enumerate available SQL instances
Write-Host 'Detecting SQL Server instances...'

$instances = Invoke-Command -NoNewScope -ScriptBlock {

    Get-ChildItem -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\' |
        Where-Object {
        $_.Name -imatch 'MSSQL[_\d]+\.SQL.*'
    } |
        ForEach-Object {

        $instance = (Get-ItemProperty $_.PSPath).'(default)'

        if ([string]::IsNullOrEmpty($instance) -or $instance -eq '.')
        {
            $instanceName = 'DEFAULT'
            $instance = '.'
        }
        else
        {
            $instanceName = $instance
        }

        $connectionString = $(
            if (${ENV:BHBuildSystem} -ieq 'AppVeyor')
            {
                "Server=(local)\$instance;User ID=sa;Password=Password12!"
            }
            else
            {
                "Server=(local)\$instance;Integrated Security=true"
            }
        )
        Write-Host -NoNewline "- Found $instance. Getting details... "
        Get-SqlServerInstanceData -InstanceName $instanceName -ConnectionString $connectionString
    }

    # Enumerate localdb instances
    'v11.0', 'MSSQLLocalDB' |
        ForEach-Object {

        $instance = "(localdb)\$_"
        Write-Host "Checking for $instance ... "
        Get-SqlServerInstanceData -InstanceName $instance -ConnectionString "Server=$instance;Integrated Security=true";
    }
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
                    Set-TestInconclusive -Message 'Full Text Indexing not supported on this instance'
                }
            }
        }
    }
}

Describe 'Basic SQL Server Provider Tests' {
    $sqlServerProviderInstalled = Import-SqlServerProvider

    if (-not $sqlServerProviderInstalled)
    {
        Write-Warning 'Unable to install SQL Server Provider'
    }

    $instances |
        Where-Object { $_.Instance -inotlike '*(localdb)*' } | # locadb not available through provider
        ForEach-Object {

        $instanceInfo = $_

        Context $instanceInfo.Instance {

            It 'Connects using the provider context' {

                if ($sqlServerProviderInstalled)
                {
                    try
                    {
                        Push-Location "SQLSERVER:\SQL\${env:COMPUTERNAME}\$($instanceInfo.Instance)"

                        Invoke-SqlExecute -Query 'SELECT @@VERSION'
                    }
                    finally
                    {
                        Pop-Location
                    }
                }
                else
                {
                    Set-TestInconclusive -Message 'No SQL Server provider available'
                }
            }
        }
    }
}
