$ErrorActionPreference = 'Stop'

try
{
	$cloneDir = Join-Path ${env:TEMP} TestData
	New-Item -Path $cloneDir -ItemType Directory | Out-Null
	Push-Location $cloneDir
	git clone -q -n https://github.com/Microsoft/sql-server-samples 
	cd sql-server-samples 
	git config core.sparsecheckout true
	'samples/databases/adventure-works/*' | Out-File -Append -Encoding ascii .git/info/sparse-checkout 
	git checkout -q
	Pop-Location
}
catch
{
	Write-Host -ForegroundColor Red $_.Exception.Message
	$_.ScriptStackTrace
	exit 1
}