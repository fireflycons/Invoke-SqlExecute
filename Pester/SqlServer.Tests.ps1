# Pester tests for the module

# Dot-source helpers
. "$PSScriptRoot\TestHelpers.ps1"

$ModuleName = 'Firefly.InvokeSqlExecute'

# Name of test DB to create
$testDatabase = 'Test-InvokeSqExecute'

# Directory where outputs with -OutFile or output redirections go
$testOutputRoot = "$PSScriptRoot\TestOutputs"

# Clear any previous test output
if (Test-Path -Path $testOutputRoot -PathType Container)
{
    Remove-Item -Path $testOutputRoot -Recurse -Force
}

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
    <#'v11.0',#> 'MSSQLLocalDB' |
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

            Invoke-SqlExecute -ConnectionString $instances[0].Connection -InputFile $connectTest -OutputAs Text -OutputFile (Get-TestOutputPath -TestOutputDirectory $testOutputRoot)
        }

        It 'Should throw if server does not exist' {

            { Invoke-SqlExecute -ConnectionString $instances[0].Connection -Query ":CONNECT LJSDFGPDFK"  -OutputAs Text -OutputFile (Get-TestOutputPath -TestOutputDirectory $testOutputRoot) @suppressConsole } | Should Throw
        }

        It 'Should throw with invalid credentials' {

            $cb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder ($instances[0].Connection)
            { Invoke-SqlExecute -ConnectionString $instances[0].Connection -Query ":CONNECT $($cb.DataSource) -U adssad -P dfsgsdfsd"  -OutputAs Text -OutputFile (Get-TestOutputPath -TestOutputDirectory $testOutputRoot) @suppressConsole } | Should Throw
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

    Context ':LISTVAR' {

        It 'Should list defined scripting variables' {

            $outFile = Get-TestOutputPath -TestOutputDirectory $testOutputRoot

            $vars = @{
                VAR1 = 'Value1'
                VAR2 = 'Value2'
                VAR3 = 'Value2'
            }

            Invoke-SqlExecute -ConnectionString $firstInstance.Connection -Variable $vars -Query "USE [tempdb]`nGO`n:SETVAR VAR4 `"Value4`"`n:LISTVAR" -Verbose -OutputFile $outFile
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

            Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\Should_process_included_file_successfully.sql" -OutputAs Scalar -OutputFile (Get-TestOutputPath -TestOutputDirectory $testOutputRoot) | Should Be 1
        }

        It 'Should report exception with detail of included file' {

            $ex = $null

            try
            {
                Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\Should_report_exception_with_detail_of_included_file.sql"  -OutputFile (Get-TestOutputPath -TestOutputDirectory $testOutputRoot) @suppressConsole
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
            "@echo off`necho $guid > `"${env:TEMP}\testcmd.txt`"" | Out-File "${env:TEMP}\testcmd.bat" -Encoding ascii

            Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection)" -Query ":!! `"${env:TEMP}\testcmd.bat`""

            # Trim pesky space added by dos echo from the line before testing
            (Get-Content "${env:TEMP}\testcmd.txt" | Select-Object -First 1).Trim() | Should Be $guid
        }
    }

    Context ':OUT, :ERROR' {

        It 'Should redirect stdout to a file' {

            $outFile = Get-TestOutputPath -TestOutputDirectory $testOutputRoot
            "PRINT 'Not in file'" | Out-File "${env:TEMP}\Should_redirect_stdout_to_a_file.sql" -Encoding ascii
            "GO" | Out-File "${env:TEMP}\Should_redirect_stdout_to_a_file.sql" -Encoding ascii -Append
            ":OUT `"$outFile`"" | Out-File "${env:TEMP}\Should_redirect_stdout_to_a_file.sql" -Encoding ascii -Append
            "PRINT 'In file'" | Out-File "${env:TEMP}\Should_redirect_stdout_to_a_file.sql" -Encoding ascii -Append

            Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection)" -InputFile "${env:TEMP}\Should_redirect_stdout_to_a_file.sql"
            (Get-Content $outFile | Select-Object -First 1) | Should Match '^In file'
        }

        It 'Should redirect stderr to a file' {

            $errFile = Get-TestOutputPath -TestOutputDirectory $testOutputRoot
            "PRINT 'Not in file'" | Out-File "${env:TEMP}\Should_redirect_stderr_to_a_file.sql" -Encoding ascii
            "GO" | Out-File "${env:TEMP}\Should_redirect_stderr_to_a_file.sql" -Encoding ascii -Append
            ":ERROR `"$errFile`"" | Out-File "${env:TEMP}\Should_redirect_stderr_to_a_file.sql" -Encoding ascii -Append
            "RAISERROR (N'Error in file', 16, 1)" | Out-File "${env:TEMP}\Should_redirect_stderr_to_a_file.sql" -Encoding ascii -Append

            { Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection)" -InputFile "${env:TEMP}\Should_redirect_stderr_to_a_file.sql" } | Should Throw
            $errFile | Should -FileContentMatch 'Error in file'
        }
    }

    Context ':EXIT' {

        It 'Should exit immediately for :EXIT' {

            { Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection)" -Query "RAISERROR (N'Should not see this', 16, 1)`n:EXIT`nGO`nRAISERROR (N'Or this', 16, 1)" } | Should Not Throw
        }

        It 'Should execute batch then exit for :EXIT()' {

            { Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection)" -Query "RAISERROR (N'Should see this', 16, 1)`n:EXIT()`nGO`nRAISERROR (N'But not this', 16, 1)" } | Should Throw
        }

        It 'Should set LASTEXITCODE for :EXIT(query returning int)' {

            Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection)" -Query ":EXIT(SELECT 10 AS [ExitCode])"
            $LASTEXITCODE | Should Be 10
        }

        It 'Should not set LASTEXITCODE for :EXIT(query returning non-int)' {

            Invoke-SqlExecute -ConnectionString "$($firstInstance.Connection)" -Query ":EXIT(SELECT 'a string' AS [ExitCode])"
            $LASTEXITCODE | Should Be 0
        }
    }
}

Describe 'Deploy databases from script' {

    $instances |
        Foreach-Object {

        $instanceInfo = $_

        Context $instanceInfo.Instance {

            It 'Creates AdventureWorks OLTP Database' {

                if ($instanceInfo.IsFullTextInstalled)
                {
                    $outFile = Get-TestOutputPath -TestOutputDirectory $testOutputRoot

                    {
                        Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $awDirs.OltpDir 'instawdb.sql') -Variable @{ SqlSamplesSourceDataPath = "$($awDirs.OltpDir)\" } -OverrideScriptVariables -OutputAs None -OutputFile $outFile
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
                    $outFile = Get-TestOutputPath -TestOutputDirectory $testOutputRoot

                    {
                        Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $awDirs.DwDir 'instawdbdw.sql') -Variable @{ SqlSamplesSourceDataPath = "$($awDirs.DwDir)\" } -OverrideScriptVariables -OutputAs None -OutputFile $outFile
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
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\Should_correctly_RAISERROR_when_database_set_to_single_user_mode.sql"  -OutputAs Text -OutputFile (Get-TestOutputPath -TestOutputDirectory $testOutputRoot) @suppressConsole
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
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\Should_report_stored_procedure_details_in_error_raised_within_an_executing_procedure.sql"  -OutputAs Text -OutputFile (Get-TestOutputPath -TestOutputDirectory $testOutputRoot) @suppressConsole
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
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -Query 'SELECT convert(int,100000000000)'  -OutputAs Text -OutputFile (Get-TestOutputPath -TestOutputDirectory $testOutputRoot) @suppressConsole
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
                    Invoke-SqlExecute -ConnectionString "$($instanceInfo.Connection);Database=$testDatabase" -InputFile "$PSScriptRoot\TestResources\RunStackOverflow33271446.sql"  -OutputAs Text -OutputFile (Get-TestOutputPath -TestOutputDirectory $testOutputRoot) @suppressConsole
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

Describe 'Parallel Execution' {

    Context 'Multiple connections with -Query' {

        It 'Should connect to all databases and run simple query in parallel' {

            $outFile = Get-TestOutputPath -TestOutputDirectory $testOutputRoot

            {
                Invoke-SqlExecute -ConnectionString $instances.Connection  -Parallel -Query "SELECT @@SERVERNAME AS [InstanceName]" -OutputAs Text -Verbose -OutputFile $outFile
            } |
                Should Not Throw
        }
    }

    Context 'Multiple connections with -InputFile' {

        It 'Should run database creation script on multiple instances in parallel' {

            $connections = $instances | Where-Object { $_.IsFullTextInstalled } | Select-Object -ExpandProperty Connection

            if (($connections | Measure-Object).Count -eq 0)
            {
                Set-TestInconclusive -Message "No instances on this machine support full text indexing"
            }
            else
            {
                $outFile = Get-TestOutputPath -TestOutputDirectory $testOutputRoot

                {
                    Invoke-SqlExecute -Parallel -ConnectionString $connections -InputFile (Join-Path $awDirs.DwDir 'instawdbdw.sql') -Variable @{ SqlSamplesSourceDataPath = "$($awDirs.DwDir)\" } -OverrideScriptVariables -OutputAs Text -Verbose -OutputFile $outFile
                } |
                    Should Not Throw
            }
        }

        It 'Should run different scripts with differerent connections' {

            if ((($instances | Measure-Object).Count -le 1))
            {
                Set-TestInconclusive -Message "Insufficient SQL server instances to run this test"
            }
            else
            {
                # Set up a test script for each instance
                $testscripts = $instances |
                ForEach-Object {

                    $scriptFile = Join-Path ${env:TEMP} (Get-SanitizedFileName -Text ((Get-TestName) + '_' + $_.Instance + '.sql'))
                    "SELECT @@SERVERNAME AS [ServerName]" | Out-File $scriptFile -Encoding ascii
                    $scriptFile
                }

                $outFile = Get-TestOutputPath -TestOutputDirectory $testOutputRoot

                {
                    Invoke-SqlExecute -Parallel -ConnectionString $instances.Connection -InputFile $testScripts -OutputAs Text -Verbose -OutputFile $outFile
                } |
                    Should Not Throw
            }
        }
    }

    Context 'Console Message Handler' {

        It 'Should capture output and redirect to scriptblock' {

            # Output file that the script block will send output to
            $outFile = Get-TestOutputPath -TestOutputDirectory $testOutputRoot

            if (Test-Path -Path $outFile -PathType Leaf)
            {
                Remove-Item $outfile
            }

            {
                Invoke-SqlExecute -ConnectionString $instances.Connection  -Parallel -Query "SELECT @@SERVERNAME AS [InstanceName]" -OutputAs Text -Verbose -ConsoleMessageHandler { $OutputMessage.Message | Out-File -Append $outFile }
            } |
                Should Not Throw

            $outFile | Should Exist
        }
    }
}
