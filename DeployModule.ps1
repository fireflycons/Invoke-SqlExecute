try
{
    Set-BuildEnvironment -ErrorAction SilentlyContinue

    Invoke-psake -buildFile $ENV:BHProjectPath\psake.ps1 -taskList 'Deploy' -nologo
    exit ( [int]( -not $psake.build_success ) )
}
catch
{
    Write-Error $_.Exception.Message

    # Make AppVeyor fail the build if this setup borks
    exit 1
}
