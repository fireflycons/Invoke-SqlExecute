# Invoke-SqlExecute
A complete replacement for Invoke-Sqlcmd/SQLCMD.exe with bugs in the former addressed

Work In Progress

## Specific Bugs Addressed

* [Invoke-Sqlcmd and error results](https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html)
* [Invoke-Sqlcmd runs script twice](https://stackoverflow.com/questions/33271446/invoke-sqlcmd-runs-script-twice/)
* `IncludeSqlUserErrors` switch (I think this is a bug): In the MS implementation, this parameter forces a datareader with no returned rows to iterate all avaialble result sets in the batch. This is the only way an error raised on any statement within the batch other than the first one will raise a `SqlException`. This parameter is provided for command line compatibility with Invoke-Sqlcmd, but the execution engine behaves as though it is always set.
