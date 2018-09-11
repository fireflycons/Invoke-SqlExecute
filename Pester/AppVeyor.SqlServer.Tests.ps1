# Pester tests for the module
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
        $rdr.Read()

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
        Write-Host -ForegroundColor Red "Inactive"
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

try
{
	Push-Location $env:TEMP
	Write-Host "git clone -q -n https://github.com/Microsoft/sql-server-samples"
	git clone -q -n https://github.com/Microsoft/sql-server-samples 2>&1 | ForEach-Object { $_.ToString() }
	Set-Location sql-server-samples 
	Write-Host "git config core.sparsecheckout true"
	git config core.sparsecheckout true 2>&1 | ForEach-Object { $_.ToString() }
	Write-Host "git config core.autocrlf true"
	git config core.autocrlf true 2>&1 | ForEach-Object { $_.ToString() }
	'samples/databases/adventure-works/*' | Out-File -Append -Encoding ascii .git/info/sparse-checkout 
	Write-Host "git checkout -q"
	git checkout -q 2>&1 | ForEach-Object { $_.ToString() }

    $adventureWorksOltp = Join-Path $env:TEMP 'sql-server-samples\samples\databases\adventure-works\oltp-install-script'
    $adventureWorksDw = Join-Path $env:TEMP 'sql-server-samples\samples\databases\adventure-works\data-warehouse-install-script'

	if (-not ((Test-Path -Path $adventureWorksOltp -PathType Container) -and (Test-Path $adventureWorksDw -PathType Container)))
	{
		Write-Warning 'AdventureWorks not cloned as expected'
	}
}
catch
{
	Write-Host -ForegroundColor Red "Error downloading AdventureWorks: $($_.Exception.Message)"
	$_.ScriptStackTrace
	
}
finally
{
	Pop-Location
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

                {
                    Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $adventureWorksOltp 'instawdb.sql') -IntialVariables @{ SqlSamplesSourceDataPath = "$adventureWorksOltp\" } -OverrideScriptVariables
                } |
                Should Not Throw
            }

            It 'Creates AdventureWorks Data Warehouse' {

                {
                    Invoke-SqlExecute -ConnectionString $instanceInfo.Connection -InputFile (Join-Path $adventureWorksDw 'instawdbdw.sql') -IntialVariables @{ SqlSamplesSourceDataPath = "$adventureWorksDw\" } -OverrideScriptVariables
                } |
                Should Not Throw
            }
        }
    }
}
