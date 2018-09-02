# Invoke-SqlExecute

*Work In Progress*

A complete replacement for Invoke-Sqlcmd with bugs in the former addressed. The code has no external dependencies and should 
work with whatever SMO version it finds when running within the PowerShell SQL provider context.

This implementation also includes support for nearly all the non-interactive 'colon directives' that are available in sqlcmd.exe such as `:CONNECT`, `:OUT` etc.

## Supported Environments

CI builds are run against the following SQL server versions. This is not to say that other versions won't work,
however they aren't present in the AppVeyor CI system.

* Microsoft SQL Server 2014 - 12.0.4100.1
* Microsoft SQL Server 2016 (SP1-CU8) (KB4077064) - 13.0.4474.0
* Microsoft SQL Server 2017 (RTM) - 14.0.1000.169

## Specific Bugs Addressed

* [Invoke-Sqlcmd and error results](https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html)
* [Invoke-Sqlcmd runs script twice](https://stackoverflow.com/questions/33271446/invoke-sqlcmd-runs-script-twice/)
* `-IncludeSqlUserErrors` switch (I think this is a bug): In the MS implementation, this parameter forces a datareader with no returned rows to iterate all available result sets in the batch. This is the only way an error raised on any statement within the batch other than the first one will raise a `SqlException`. This parameter is provided for command line compatibility with Invoke-Sqlcmd, but the execution engine behaves as though it is always set.

If you are aware of any other bugs or inconsistencies in Invoke-Sqlcmd where the behaviour does not align
with that of SSMS or sqlcmd.exe, please contact me and I will correct it in this implementation or fork it 
and submit a PR.
