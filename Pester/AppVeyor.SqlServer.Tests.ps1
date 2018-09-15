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

Describe 'Known Invoke-Sqlcmd bags are fixed in this implementation' {

    $instances |
        Foreach-Object {

        $instanceInfo = $_
        $testDatabase = 'Test-InvokeSqExecute'

        Context $instanceInfo.Instance {

            BeforeEach {

                # Create test database
                Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile "$PSScriptRoot\TestInitialize.sql"
            }

            AfterEach {

                # Drop test database
                Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -Query "DROP DATABASE [$testDatabase]"
            }

            It 'Raises an error when run against single user database' {

                # https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html
                # Issue #1
                #
                # Personally I think the real issue is that in the example on the web site,
                # there's no way to tell Invoke-Sqlcmd to exit on the first error as it doesn't support :on error

                $ex = $null

                try
                {
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\InvokeSqlcmdDoesNotReturnRaisedErrorIfQueryWasRunInSingleUserMode.sql" -ConsoleMessageHandler {}
                }
                catch
                {
                    $ex = $_.Exception
                }

                if ($null -eq $ex)
                {
                    throw 'Exception not raised by command'
                }

                # Now verify we got the correct SQL exception
                $ex.ErrorCount | Should Be 1
                $ex.SqlExceptions | Select-Object -First 1 | Select-Object -ExpandProperty Message | Should Be 'First Error.'
            }

            It 'Raises correct message when there is an error executing a stored procedure' {

                # https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html
                # Issue #2

                $ex = $null

                try
                {
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure.sql" -ConsoleMessageHandler {}
                }
                catch
                {
                    $ex = $_.Exception
                }

                if ($null -eq $ex)
                {
                    throw 'Exception not raised by command'
                }

                # Now verify we got the correct SQL exception
                $ex.ErrorCount | Should Be 1
                $sqlException = $ex.SqlExceptions | Select-Object -First 1
                $sqlException | Select-Object -ExpandProperty Number | Should Be 515 # Cannot insert the value null ...
                $sqlException | Select-Object -ExpandProperty Procedure | Should Be 'geterror'

            }

            It 'Raises an error on arithmetic overflow' {

                # https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html
                # Issue #3

                $ex = $null

                try
                {
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -Query 'SELECT convert(int,100000000000)' -ConsoleMessageHandler {}
                }
                catch
                {
                    $ex = $_.Exception
                }

                if ($null -eq $ex)
                {
                    throw 'Exception not raised by command'
                }

                # Now verify we got the correct SQL exception
                $ex.ErrorCount | Should Be 1
                $ex.SqlExceptions | Select-Object -First 1 | Select-Object -ExpandProperty Number | Should Be 8115 # SQL message number for arithmetic overflow
            }

            It 'Does not erroneously insert 2 records (Stack Overflow 33271446)' {

                # https://stackoverflow.com/questions/33271446/invoke-sqlcmd-runs-script-twice

                $ex = $null

                try
                {
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\RunStackOverflow33271446.sql" -ConsoleMessageHandler {}
                }
                catch
                {
                    $ex = $_.Exception
                }

                if ($null -eq $ex)
                {
                    throw 'Exception not raised by command'
                }

                # Now verify we got the correct SQL exception
                $ex.ErrorCount | Should Be 1
                $sqlException = $ex.SqlExceptions | Select-Object -First 1
                $sqlException | Select-Object -ExpandProperty Number | Should Be 515 # Cannot insert the value null ...

                # Should be one row inserted
                Invoke-RawQueryScalar -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -Query 'select count(*) from [dbo].[s]' | Should Be 1
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
                        Push-Location "SQLSERVER:\SQL\${env:COMPUTERNAME}\$($instanceInfo.Instance)\Databases\master"

                        Invoke-SqlExecute -Query 'SELECT @@SERVERNAME AS [Instance], DB_NAME() AS [CurrentDatabase]' | Out-String | Out-Host
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
