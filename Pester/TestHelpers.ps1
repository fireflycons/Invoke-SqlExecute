function Grant-DirectoryAccess
{
    param
    (
        [string]$Directory
    )

    $everyoneSid = New-Object System.Security.Principal.SecurityIdentifier ([System.Security.Principal.WellKnownSidType]::WorldSid, $null)
    $rule = New-Object  System.Security.AccessControl.FileSystemAccessRule($everyoneSid, "ReadAndExecute", "ObjectInherit, ContainerInherit", "NoPropagateInherit", "Allow")
    $acl = Get-Acl -Path $Directory
    $acl.AddAccessRule($rule) | Out-Null
    Set-Acl -Path $Directory -AclObject $acl | Out-Null
}

function Get-AdventureWorksClone
{
    Write-Host 'Cloning AdventureWorks from https://github.com/Microsoft/sql-server-samples'
    try
    {
        Push-Location $env:TEMP

        if (Test-Path -Path 'sql-server-samples\.git' -PathType Container)
        {
            Write-Host 'Repo exists. Checking it is up to date'
            Set-Location sql-server-samples
            Write-Host "git pull"
            git pull -q 2>&1 | ForEach-Object { $_.ToString() } | Out-Host
        }
        else
        {
            Write-Host "git clone -q -n https://github.com/Microsoft/sql-server-samples"
            git clone -q -n https://github.com/Microsoft/sql-server-samples 2>&1 | ForEach-Object { $_.ToString() } | Out-Host
            Set-Location sql-server-samples
            Write-Host "git config core.sparsecheckout true"
            git config core.sparsecheckout true 2>&1 | ForEach-Object { $_.ToString() } | Out-Host
            Write-Host "git config core.autocrlf true"
            git config core.autocrlf true 2>&1 | ForEach-Object { $_.ToString() } | Out-Host
            'samples/databases/adventure-works/*' | Out-File -Append -Encoding ascii .git/info/sparse-checkout
        }

        Write-Host "git checkout -q"
        git checkout -q 2>&1 | ForEach-Object { $_.ToString() } | Out-Host

        $adventureWorksOltp = Join-Path $env:TEMP 'sql-server-samples\samples\databases\adventure-works\oltp-install-script'
        $adventureWorksDw = Join-Path $env:TEMP 'sql-server-samples\samples\databases\adventure-works\data-warehouse-install-script'

        if (-not ((Test-Path -Path $adventureWorksOltp -PathType Container) -and (Test-Path $adventureWorksDw -PathType Container)))
        {
            Write-Warning 'AdventureWorks not cloned as expected'
        }
        else
        {
            # Grant access to Everyone on the files so that SQL server service account can read
            Grant-DirectoryAccess -Directory $adventureWorksOltp
            Grant-DirectoryAccess -Directory $adventureWorksDw
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

    New-Object PSObject -Property @{
        OltpDir = $adventureWorksOltp
        DwDir   = $adventureWorksDw
    }
}

function Get-SqlServerInstanceData
{
    <#
    .SYNOPSIS
        Given a connection string, test connection and get server properties
        required for test runs

    .PARAMETER InstanceName
        Descriptive instance name#

    .PARAMETER ConnectionString
        Connection string to try

    .OUTPUTS
        [object] with the following fields, or nothing if connection fails

        Instance            - Name of instance
        COnnection          - Connection string passed in
        IsFullTextInstalled - True if full text is installed on instance; else false
    #>

    param
    (
        [string]$InstanceName,
        [string]$ConnectionString
    )
    try
    {
        $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandType = 'Text'
        $cmd.CommandText = "SELECT @@VERSION AS [Version], FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') AS [IsFullTextInstalled]"
        $rdr = $cmd.ExecuteReader()
        $rdr.Read() | Out-Null

        $i = New-Object PSObject -Property @{
            Instance            = $InstanceName
            Connection          = $ConnectionString
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
        if ($_.Exception.Message -ilike '*Cannot create an automatic instance*')
        {
            Write-Host "`tNot found."
        }
        else
        {
            Write-Host -ForegroundColor Red $_.Exception.Message
        }
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

function Import-SqlServerProvider
{
    <#
        .SYNOPSIS
            Try to load a SQL Server PS provider by any means :-)
    #>

    foreach ($module in @('SqlServer', 'SQLPS'))
    {
        if (Get-Module -ListAvailable | Where-Object { $_.Name -ieq $module})
        {
            Import-Module -Global $module -Verbose:$false
            return $true
        }
    }

    # Wasn't found locally, try to install from the gallery

    try
    {
        Install-Module SqlServer -force -AllowClobber -Scope CurrentUser
        Import-Module -Global SqlServer -Verbose:$false
        return $true
    }
    catch
    {
        return $false
    }
}

function Invoke-RawQueryScalar
{
    <#
        .SYNOPSIS
            Execute a query with a scalar result using .NET API, so as not to rely on the cmdlet under test

        .PARAMETER ConnectionString
            The connection string

        .PARAMETER Query
            SQL to execute

        .OUTPUTS
            [object] Scalar result
    #>

    param
    (
        [string]$ConnectionString,
        [string]$Query
    )

    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $cmd = $null

    try
    {
        $conn.Open()
        [System.Data.SqlClient.SqlConnection]::ClearPool($conn)
        $cmd = $conn.CreateCommand()
        $cmd.CommandType = 'Text'
        $cmd.CommandText = $Query
        $cmd.ExecuteScalar()
    }
    finally
    {
        ($cmd, $conn) |
            Where-Object { $null -ne $_ } |
            ForEach-Object {
                $_.Dispose()
            }
    }
}

function Get-TestName
{
    <#
        .SYNOPSIS
            Get name of enclosing It block
    #>

    $test = Get-PSCallStack | Where-Object { $_.Command -ieq 'It' }

    if (-not $test)
    {
        throw 'Get-TestPath must be called from within It {} block.'
    }

    $test.InvocationInfo.BoundParameters['name']
}

function Get-TestPath
{
    <#
        .SYNOPSIS
            Get name of enclosing It block

        .PARAMETER Sanitize
            Sanitize test name by replacing invalid filename characters with underscores
    #>

    param
    (
        [switch]$Sanitize
    )

    $testName = Get-TestName
    $context = Get-PSCallStack | Where-Object { $_.Command -ieq 'Context' }
    $describe = Get-PSCallStack | Where-Object { $_.Command -ieq 'Describe' }

    $contextName = $context.InvocationInfo.BoundParameters['name']
    $describeName = $describe.InvocationInfo.BoundParameters['name']

    if ($Sanitize)
    {
        $invalidChars = [IO.Path]::GetInvalidFileNameChars()

        (
            $describeName, $contextName, $testName |
            ForEach-Object {

                Get-SanitizedFilename -Text $_
            }
        ) -join '\'
    }
    else {
        $describeName + '\' + $contextName + '\' + $testName
    }
}

function Get-SanitizedFilename
{
    param
    (
        [string]$Text
    )

    foreach ($c in [IO.Path]::GetInvalidFileNameChars())
    {
        $Text = $Text.Replace($c, '_')
    }

    $Text
}

function Get-TestOutputPath
{
    <#
        .SYNOPSIS
            Create path to test output file, creating directories as needed

        .PARAMETER TestOutputDirectory
            Root path for test outputs
    #>
    param
    (
        [string]$TestOutputDirectory
    )

    $testOutputFile = Join-Path $TestOutputDirectory (Get-TestPath -Sanitize)
    $dir = Split-Path -Parent $testOutputFile

    if (-not (Test-Path -Path $dir -PathType Container))
    {
        New-Item -Path $dir -ItemType Container | Out-Null
    }

    $testOutputFile + '.txt'
}