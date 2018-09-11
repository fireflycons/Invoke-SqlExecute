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
    $connection = "Server=(local)\$instance;User ID=sa;Password=Password12!"

    Write-Host -NoNewline "- Found $instance. Getting details... "

    try
    {
        $conn = New-Object System.Data.SqlClient.SqlConnection $connection
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandType = 'Text'
        $cmd.CommandText = "SELECT @@VERSION AS [Version], FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') AS [IsFullTextInstalled]"
        $rdr = $cmd.ExecuteReader()
        $rdr.Read() | Out-Null

        $i = New-Object PSObject -Property @{
            Instance            = $instance
            Connection          = $connection
            IsFullTextInstalled = $rdr['IsFullTextInstalled'] -ne 0
        }

        Write-Host

        $rdr['Version'] -split [Environment]::NewLine |
            ForEach-Object {
            Write-Host "    $_"
        }

        Write-Host "    Full Text Installed: $($i.IsFullTextInstalled)"
        Write-Host
                    
        $i
    }
    catch
    {
        Write-Host -ForegroundColor Red $_.Exception.Message
    }
    finally
    {
        ($rdr, $cmd, $conn) |
            Foreach-Object {
            if ($_)
            {
                $_.Dispose()
            }
        }
    }
}

if (($instances | Measure-Object).Count -eq 0)
{
    Write-Warning "No SQL Server instances found"
}


# Make sure one or multiple versions of the module are not loaded
Get-Module -Name $ModuleName | Remove-Module

# Find the Manifest file
$ManifestFile = "$(Split-path (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition))\$ModuleName\$ModuleName.psd1"

# Import the module
Import-Module -Name $ManifestFile

$awDirs = Get-AdventureWorksClone

Describe 'AdventureWorks Database Creation' {

    $instances | 
    Foreach-Object {

        $instanceInfo = $_

        Context $instanceInfo.Instance {

            It 'Creates AdventureWorks OLTP Database' {

                {
                    Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $awDirs.OltpDir 'instawdb.sql') -Variable @{ SqlSamplesSourceDataPath = "$($awDirs.OltpDir)\" } -OverrideScriptVariables
                } |
                Should Not Throw
            }

            It 'Creates AdventureWorks Data Warehouse' {

                {
                    Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $awDirs.DwDir 'instawdbdw.sql') -Variable @{ SqlSamplesSourceDataPath = "$($awDirs.DwDir)\" } -OverrideScriptVariables
                } |
                Should Not Throw
            }
        }
    }
}
