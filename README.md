# Invoke-SqlExecute

|Branch|Status|
|------|------|
|master|[![Build status](https://ci.appveyor.com/api/projects/status/1p6dvf2gldjj1t1h/branch/master?svg=true)](https://ci.appveyor.com/project/fireflycons/invoke-sqlexecute/branch/master)|
|dev|[![Build status](https://ci.appveyor.com/api/projects/status/1p6dvf2gldjj1t1h/branch/dev?svg=true)](https://ci.appveyor.com/project/fireflycons/invoke-sqlexecute/branch/dev)|

*Work In Progress - there may well be bugs!*

You are strongly advised to test thoroughly within a non-production environment before letting this loose on any production system!

TL;DR - Jump to [Invoke-SqlExecute command syntax](./docs/en-US/Invoke-SqlExecute.md)

A complete replacement for the Invoke-Sqlcmd cmdlet with bugs in the former addressed. The code has no external dependencies and should work with whatever SMO version it finds when running within the PowerShell SQL provider context.

Another big bugbear of mine is the tersity of error messages returned by the standard implementations. SQL server only knows about the currently executing batch and thus the error details will refer to the line number within the batch. If you have a huge SQL file with hundreds or thousands of batches in, it is more useful to know which line within the *entire* input file is the one with the error. The parser in this implementation tracks the line number of the start of each batch within the input and adds this to the line number reported by SQL server to give you the true location of the erroneous statement as near as possible, and that includes descent into files refrenced with `:r` - for instance:

```
SqlException caught!
Client:              APPVYR-WIN
Server:              (local)\SQL2017
Batch:               C:\Users\appveyor\AppData\Local\Temp\1\sql-server-samples\samples\databases\adventure-works\data-warehouse-install-script\instawdbdw.sql, beginning at line 738
Message:             Cannot bulk load because the file "C:\Users\appveyor\AppData\Local\Temp\1\sql-server-samples\samples\databases\adventure-works\data-warehouse-install-script\DimAccount.csv" could not be opened. Operating system error code 5(Access is denied.).
Source:              .Net SqlClient Data Provider
Number:              4861
Class:               16
State:               1
Line (within batch): 12
Line (within file):  749
Error near:
BULK INSERT [dbo].[DimAccount] FROM 'C:\Users\appveyor\AppData\Local\Temp\1\sql-server-samples\samples\databases\adventure-works\data-warehouse-install-script\DimAccount.csv'
WITH (
    CHECK_CONSTRAINTS,
   -- CODEPAGE='ACP',
    DATAFILETYPE = 'widechar',
    FIELDTERMINATOR= '|',
    ROWTERMINATOR = '\n',
    KEEPIDENTITY,
    TABLOCK
);
```

This implementation also includes support for more non-interactive 'colon commands' than are available in Invoke-sqlcmd such as `:CONNECT`, `:OUT`, `:!!` etc.


## Supported Environments

CI builds are run against the following SQL server versions. This is not to say that other versions won't work, however they aren't present in the AppVeyor CI system.

* Microsoft SQL Server 2014 - 12.0.4100.1
* Microsoft SQL Server 2016 (SP1-CU8) (KB4077064) - 13.0.4474.0
* Microsoft SQL Server 2017 (RTM) - 14.0.1000.169

## Specific Bugs Addressed

* [Invoke-Sqlcmd and error results](https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html)
* [Invoke-Sqlcmd runs script twice](https://stackoverflow.com/questions/33271446/invoke-sqlcmd-runs-script-twice/)
* `-IncludeSqlUserErrors` switch (I think this is a bug): In the MS implementation, this parameter forces a datareader with no returned rows to iterate all available result sets in the batch. This is the only way an error raised on any statement within the batch other than the first one will raise a `SqlException`. This parameter is provided for command line compatibility with Invoke-Sqlcmd, but the execution engine behaves as though it is always set.

If you are aware of any other bugs or inconsistencies in Invoke-Sqlcmd where the behaviour does not align with that of SSMS or sqlcmd.exe, please contact me and I will correct it in this implementation or you can fork it and submit a PR.



