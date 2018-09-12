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
        Write-Host "git clone -q -n https://github.com/Microsoft/sql-server-samples"
        git clone -q -n https://github.com/Microsoft/sql-server-samples 2>&1 | ForEach-Object { $_.ToString() } | Out-Host
        Set-Location sql-server-samples
        Write-Host "git config core.sparsecheckout true"
        git config core.sparsecheckout true 2>&1 | ForEach-Object { $_.ToString() } | Out-Host
        Write-Host "git config core.autocrlf true"
        git config core.autocrlf true 2>&1 | ForEach-Object { $_.ToString() } | Out-Host
        'samples/databases/adventure-works/*' | Out-File -Append -Encoding ascii .git/info/sparse-checkout
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