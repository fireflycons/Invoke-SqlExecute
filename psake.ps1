# PSake makes variables declared here available in other scriptblocks
# Init some things
Properties {
    # Find the build folder based on build system
    $ProjectRoot = $ENV:BHProjectPath
    if (-not $ProjectRoot)
    {
        $ProjectRoot = $PSScriptRoot
    }
    $ProjectRoot = Convert-Path $ProjectRoot

    try
    {
        $script:IsWindows = (-not (Get-Variable -Name IsWindows -ErrorAction Ignore)) -or $IsWindows
        $script:IsLinux = (Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux
        $script:IsMacOS = (Get-Variable -Name IsMacOS -ErrorAction Ignore) -and $IsMacOS
        $script:IsCoreCLR = $PSVersionTable.ContainsKey('PSEdition') -and $PSVersionTable.PSEdition -eq 'Core'
    }
    catch { }

    $Timestamp = Get-date -uformat "%Y%m%d-%H%M%S"
    $PSVersion = $PSVersionTable.PSVersion.Major
    $TestFile = "TestResults_PS$PSVersion`_$TimeStamp.xml"
    $lines = '----------------------------------------------------------------------'

    $Verbose = @{}
    if ($ENV:BHCommitMessage -match "!verbose")
    {
        $Verbose = @{Verbose = $True}
    }

    $DefaultLocale = 'en-US'
    $DocsRootDir = "$PSScriptRoot\docs"
    $ModuleName = "Firefly.InvokeSqlExecute"
    $ModuleOutDir = "$PSScriptRoot\Firefly.InvokeSqlExecute"
    $CompilationOutput = "$PSScriptRoot\Firefly.InvokeSqlExecute.PowerShell\bin"

}

Task Default -Depends PrepareModule

Task Init {
    $lines
    Set-Location $ProjectRoot
    "Build System Details:"
    Get-Item ENV:BH*
    "`n"

    if ($script:IsWindows)
    {
        "Checking for NuGet"
        $psgDir = Join-Path ${env:LOCALAPPDATA} "Microsoft\Windows\PowerShell\PowerShellGet"

        $nugetPath = $(

            $nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue

            if ($nuget)
            {
                $nuget.Path
            }
            else
            {
                if (Test-Path -Path (Join-Path $psgDir 'nuget.exe'))
                {
                    Join-Path $psgDir 'nuget.exe'
                }
            }
        )

        if ($nugetPath)
        {
            "NuGet.exe found at '$nugetPath"
        }
        else
        {
            if (-not (Test-Path -Path $psgDir -PathType Container))
            {
                New-Item -Path $psgDir -ItemType Directory | Out-Null
            }

            "Installing NuGet to '$psgDir'"
            Invoke-WebRequest -Uri https://nuget.org/nuget.exe -OutFile (Join-Path $psgDir 'nuget.exe')
        }
    }
}


Task PrepareModule -Depends BuildHelp { }

Task Build -Depends Init {

    # Gather C# compiler output. Place in module staging directory.
    # Prefer most recent build

    $lines
    "`n`tSTATUS: Copying module files to staging folder"

    $moduleSrc = Invoke-Command -NoNewScope -ScriptBlock {

        $f = 'Debug', 'Release' |
        ForEach-Object {

            $p = Join-Path $CompilationOutput "$_\Firefly.InvokeSqlExecute.PowerShell.dll"

            if (Test-Path -Path $p -PathType Leaf)
            {
                Get-Item -Path $p
            }
        } |
        Sort-Object -Descending $_.LastWriteTime |
        Select-Object -First 1

        if ($f)
        {
            $f.DirectoryName
        }
        else
        {
            throw "No outputs from C# compilation found"
        }
    }

    if ($ENV:BHBuildSystem -ine 'Unknown')
    {
        # Remove placeholder file. Don't want it packaged
        Remove-Item "$ModuleOutDir\*"
    }

    "`tTaking output from $(Split-Path -Leaf $moduleSrc)"
    '*.dll', '*.ps*', '*.dll-Help.xml' |
    ForEach-Object {

        Remove-Item -Path "$ModuleOutDir\$_"
        Copy-Item (Join-Path $moduleSrc $_) $ModuleOutDir
    }

    Set-BuildEnvironment -ErrorAction SilentlyContinue
}

Task Test -Depends Build {
    $lines
    "`n`tSTATUS: Testing with PowerShell $PSVersion"

    # Gather test results. Store them in a variable and file
    $pesterParameters = @{
        Path         = "$ProjectRoot\Pester"
        PassThru     = $true
        OutputFormat = "NUnitXml"
        OutputFile   = "$ProjectRoot\$TestFile"
    }

    if (-Not $IsWindows) { $pesterParameters["ExcludeTag"] = "WindowsOnly" }
    $TestResults = Invoke-Pester @pesterParameters

    # In Appveyor?  Upload our tests! #Abstract this into a function?
    If ($ENV:BHBuildSystem -eq 'AppVeyor')
    {
        (New-Object 'System.Net.WebClient').UploadFile(
            "https://ci.appveyor.com/api/testresults/nunit/$($env:APPVEYOR_JOB_ID)",
            "$ProjectRoot\$TestFile" )
    }

    Remove-Item "$ProjectRoot\$TestFile" -Force -ErrorAction SilentlyContinue

    # Failed tests?
    # Need to tell psake or it will proceed to the deployment. Danger!
    if ($TestResults.FailedCount -gt 0)
    {
        Write-Error "Failed '$($TestResults.FailedCount)' tests, build failed"
    }
    "`n"
}

Task UpdateModuleVersion -Depends Test {
    $lines

    # Load the module, read the exported functions, update the psd1 FunctionsToExport
    #Set-ModuleFunctions

    # Bump the module version if we didn't already
    Try
    {
        [version]$GalleryVersion = Get-NextNugetPackageVersion -Name $env:BHProjectName -ErrorAction Stop
        [version]$GithubVersion = Get-MetaData -Path $env:BHPSModuleManifest -PropertyName ModuleVersion -ErrorAction Stop
        if ($GalleryVersion -ge $GithubVersion)
        {
            Update-Metadata -Path $env:BHPSModuleManifest -PropertyName ModuleVersion -Value $GalleryVersion -ErrorAction stop
        }
    }
    Catch
    {
        "Failed to update version for '$env:BHProjectName': $_.`nContinuing with existing version"
    }
}

Task Deploy -Depends Init {
    $lines

    # Gate deployment
    if (#$true
        $ENV:BHBuildSystem -ne 'Unknown' -and
        $ENV:BHBranchName -eq "master" -and
        $ENV:BHCommitMessage -match '!deploy'
    )
    {
        $Params = @{
            Path  = $ProjectRoot
            Force = $true
        }

        Invoke-PSDeploy @Verbose @Params
    }
    else
    {
        "Skipping deployment: To deploy, ensure that...`n" +
        "`t* You are in a known build system (Current: $ENV:BHBuildSystem)`n" +
        "`t* You are committing to the master branch (Current: $ENV:BHBranchName) `n" +
        "`t* Your commit message includes !deploy (Current: $ENV:BHCommitMessage)"
    }
}

Task BuildHelp -Depends UpdateModuleVersion, GenerateMarkdown {}

Task GenerateMarkdown -requiredVariables DefaultLocale, DocsRootDir {
    if (!(Get-Module platyPS -ListAvailable))
    {
        "platyPS module is not installed. Skipping $($psake.context.currentTaskName) task."
        return
    }

    $moduleInfo = Import-Module $ENV:BHPSModuleManifest -Global -Force -PassThru

    try
    {
        if ($moduleInfo.ExportedCommands.Count -eq 0)
        {
            "No commands have been exported. Skipping $($psake.context.currentTaskName) task."
            return
        }

        if (!(Test-Path -LiteralPath $DocsRootDir))
        {
            New-Item $DocsRootDir -ItemType Directory > $null
        }

        if (Get-ChildItem -LiteralPath $DocsRootDir -Filter *.md -Recurse)
        {
            Get-ChildItem -LiteralPath $DocsRootDir -Directory |
                ForEach-Object {
                Update-MarkdownHelp -Path $_.FullName -Verbose:$VerbosePreference > $null
            }
        }

        # ErrorAction set to SilentlyContinue so this command will not overwrite an existing MD file.
        New-MarkdownHelp -Module $ModuleName -Locale $DefaultLocale -OutputFolder $DocsRootDir\$DefaultLocale `
            -WithModulePage -ErrorAction SilentlyContinue -Verbose:$VerbosePreference > $null
    }
    finally
    {
        Remove-Module $ModuleName
    }
}
