$ErrorActionPreference = 'Stop'

try
{
	$cloneDir = 'C:\TestData'
	New-Item -Path $cloneDir -ItemType Directory | Out-Null
	Push-Location $cloneDir
	Write-Host "git clone -q -n https://github.com/Microsoft/sql-server-samples"
	git clone -q -n https://github.com/Microsoft/sql-server-samples 2>&1 | % { $_.ToString() }
	cd sql-server-samples 
	Write-Host "git config core.sparsecheckout true"
	git config core.sparsecheckout true 2>&1 | % { $_.ToString() }
	Write-Host "git config core.autocrlf true"
	git config core.autocrlf true 2>&1 | % { $_.ToString() }
	'samples/databases/adventure-works/*' | Out-File -Append -Encoding ascii .git/info/sparse-checkout 
	Write-Host "git checkout -q"
	git checkout -q 2>&1 | % { $_.ToString() }

	if (-not (Test-Path -Path (Join-Path $cloneDir 'sql-server-samples\samples\databases\adventure-works\oltp-install-script')))
	{
		throw 'AdventureWorks not cloned as expected'
	}
}
catch
{
	Write-Host -ForegroundColor Red $_.Exception.Message
	$_.ScriptStackTrace
	exit 1
}
finally
{
	Pop-Location
}