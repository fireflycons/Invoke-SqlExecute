# Pester tests for the module

# Dot-source helpers
. "$PSScriptRoot\TestHelpers.ps1"

$ModuleName = 'Firefly.InvokeSqlExecute'

$testDatabase = 'Test-InvokeSqExecute'

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

# This actually tests -ConsoleMessageHandler to the extent
# that the empty scriptblock swallows all conosle ourput.
$suppressConsole = @{
    ConsoleMessageHandler = {}
}

if ($env:VerboseTests -eq 1)
{
    # If this env var is set to 1, these tests will output the SQL exception messages to the console
    $suppressConsole = @{}
}


Describe 'SQLCMD Commands' {

    # Most of these tests can be run on a single instance, as they test client-side operations

    $firstInstance = $instances |
    Select-Object -First 1

    Context ':CONNECT' {

        # Build a test script
        $connectTest = "${env:TEMP}\Invoke-SqlExecute-Connect.sql"

        if (Test-Path -Path $connectTest -PathType Leaf)
        {
            Remove-Item $connectTest
        }

        $instances |
        ForEach-Object {

            $cb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder ($_.Connection)

            if ($cb.IntegratedSecurity)
            {
                ":CONNECT $($cb.DataSource)" | Out-File $connectTest -Encoding ascii -Append
            }
            else
            {
                ":CONNECT $($cb.DataSource) -U $($cb.UserID) -P $($cb.Password)" | Out-File $connectTest -Encoding ascii -Append
            }

            "SELECT @@SERVERNAME AS [ServerName]"  | Out-File $connectTest -Encoding ascii -Append
        }

        It 'Should :CONNECT to all discovered SQL Servers' {

            Invoke-SqlExecute -ConnectionString $instances[0].Connection -InputFile $connectTest -OutputAs DataRows
        }

        It 'Should throw if server does not exist' {

            { Invoke-SqlExecute -ConnectionString $instances[0].Connection -Query ":CONNECT LJSDFGPDFK" @suppressConsole } | Should Throw
        }

        It 'Should throw with invalid credentials' {

            $cb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder ($instances[0].Connection)
            { Invoke-SqlExecute -ConnectionString $instances[0].Connection -Query ":CONNECT $($cb.DataSource) -U adssad -P dfsgsdfsd" @suppressConsole } | Should Throw
        }
    }

    Context ':SETVAR' {

        It 'Should set a variable with :SETVAR' {

            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -Query ":SETVAR TestVar `"Hello`" `nSELECT '`$(TestVar)' AS [Result]" -OutputAs Scalar | Should Be 'Hello'
        }

        It 'Should set a variable from command line with hashtable argument' {

            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -Variable @{ TestVar = 'Hello' } -Query "SELECT '`$(TestVar)' AS [Result]" -OutputAs Scalar | Should Be 'Hello'
        }

        It 'Should set a variable from command line with string argument' {

            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -Variable "MyVar1 = 'String1';MyVar2 = 'String2'" -Query "SELECT '`$(MyVar1)' AS [Result]" -OutputAs Scalar | Should Be 'String1'
        }

        It 'Should set a variable from command line with array argument' {

            $myArray = "MyVar1 = 'String1'", "MyVar2 = 'String2'"
            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -Variable $myArray -Query "SELECT '`$(MyVar1)' AS [Result]" -OutputAs Scalar | Should Be 'String1'
        }

        It 'Should not override a command line variable if -OverrideScriptVariables is set' {

            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -Variable @{ TestVar = 'Hello' } -OverrideScriptVariables -Query ":SETVAR TestVar `"Goodbye`" `nSELECT '`$(TestVar)' AS [Result]" -OutputAs Scalar | Should Be 'Hello'
        }

        It 'Should override a command line variable if -OverrideScriptVariables is not set' {

            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -Variable @{ TestVar = 'Hello' } -Query ":SETVAR TestVar `"Goodbye`" `nSELECT '`$(TestVar)' AS [Result]" -OutputAs Scalar | Should Be 'Goodbye'
        }

        It 'Should set $LASTEXITCODE with value of :SETVAR SQLCMDERRORLEVEL' {

            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -Query ':SETVAR SQLCMDERRORLEVEL "6"'

            $LASTEXITCODE | Should Be 6
        }
    }

    Context ':R (included script files)' {

        BeforeEach {

            # Create test database
            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -InputFile "$PSScriptRoot\TestResources\TestInitialize.sql"
        }

        AfterEach {

            # Drop test database
            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -Query "DROP DATABASE [$testDatabase]"
        }

        It 'Should process included file successfully' {

            Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\Should_process_included_file_successfully.sql" -OutputAs Scalar | Should Be 1
        }

        It 'Should report exception with detail of included file' {

            $ex = $null

            try
            {
                Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\Should_report_exception_with_detail_of_included_file.sql" @suppressConsole
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
            $sqlException.Number | Should Be 2627       # PK violation

            # Error context detail in the exeception's Data ditionary
            (Split-Path -Leaf $sqlException.Data['BatchSource']) | Should Be 'Should_report_exception_with_detail_of_included_file.2.sql'
            $sqlException.Data['SourceErrorLine'] | Should Be 8
        }
    }

    Context ':!! (Shell commands)' {

        It 'Should execute a shell command' {

            $guid = [Guid]::NewGuid().ToString()

            # Create a batch file to run
            "@echo off`necho $guid > `"$PSScriptRoot\testcmd.txt`"" | Out-File "$PSScriptRoot\testcmd.bat" -Encoding ascii

            Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection)" -Query ":!! `"$PSScriptRoot\testcmd.bat`""

            # Trim pesky space added by dos echo from the line before testing
            (Get-Content "$PSScriptRoot\testcmd.txt" | Select-Object -First 1).Trim() | Should Be $guid
        }
    }
}

Describe 'AdventureWorks Database Creation' {

    $instances |
        Foreach-Object {

        $instanceInfo = $_

        Context $instanceInfo.Instance {

            It 'Creates AdventureWorks OLTP Database' {

                if ($instanceInfo.IsFullTextInstalled)
                {
                    {
                        Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $awDirs.OltpDir 'instawdb.sql') -Variable @{ SqlSamplesSourceDataPath = "$($awDirs.OltpDir)\" } -OverrideScriptVariables -OutputAs None
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
                        Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $awDirs.DwDir 'instawdbdw.sql') -Variable @{ SqlSamplesSourceDataPath = "$($awDirs.DwDir)\" } -OverrideScriptVariables -OutputAs None
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

Describe 'Known Invoke-Sqlcmd bugs are fixed in this implementation' {

    $instances |
        Foreach-Object {

        $instanceInfo = $_

        Context $instanceInfo.Instance {

            BeforeEach {

                # Create test database
                Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile "$PSScriptRoot\TestResources\TestInitialize.sql"
            }

            AfterEach {

                # Drop test database
                Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -Query "DROP DATABASE [$testDatabase]"
            }

            It 'Should correctly RAISERROR when database set to single user mode' {

                # https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html
                # Issue #1

                $ex = $null

                try
                {
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\Should_correctly_RAISERROR_when_database_set_to_single_user_mode.sql" @suppressConsole
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

            It 'Should report stored procedure details in error raised within an executing procedure' {

                # https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html
                # Issue #2

                $ex = $null

                try
                {
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\Should_report_stored_procedure_details_in_error_raised_within_an_executing_procedure.sql" @suppressConsole
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
                $sqlException | Select-Object -ExpandProperty Procedure | Should Match '^(dbo\.)?geterror$' # SQL2017 returns qualified proc name

            }

            It 'Should RAISERROR on arithmetic overflow' {

                # https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html
                # Issue #3

                $ex = $null

                try
                {
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -Query 'SELECT convert(int,100000000000)' @suppressConsole
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
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\RunStackOverflow33271446.sql" @suppressConsole
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
        Where-Object { $_.Instance -inotlike '*(localdb)*' } | # localdb not available through provider
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
