# [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes((Get-Content .\DebugScript.ps1 -Raw))) | clip.exe
try 
{
	Import-Module '.\Firefly.InvokeSqlExecute.dll'
	 Invoke-SqlExecute -ConnectionString 'Server=(localdb)\mssqllocaldb' -InputFile "D:\.Dev\Git\Obsequium\Obsequium.Net\Obs.Database\bin\Debug\ObsTest.Database.publish.sql" -Verbose -OutputAs None -DryRun 
}
catch
{
	$_.Exception.Message
}
finally
{
	"LASTEXITCODE = $LASTEXITCODE"
	$x = Read-Host "continue"
}
