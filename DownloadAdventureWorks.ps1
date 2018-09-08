$ErrorActionPreference = 'Stop'

try
{
	New-Item -Path C:\TestData -ItemType Directory | Out-Null
	Push-Location C:\TestData
	git clone -n https://github.com/Microsoft/sql-server-samples 
	cd sql-server-samples 
	git config core.sparsecheckout true
	'samples/databases/adventure-works/*' | Out-File -Append -Encoding ascii .git/info/sparse-checkout 
	git checkout
	Pop-Location
}
catch
{
	Write-Host -ForegroundColor Red $_.Exception.Message
	$_.ScriptStackTrace
	exit 1
}