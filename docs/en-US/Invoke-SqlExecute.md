---
external help file: Firefly.InvokeSqlExecute.PowerShell.dll-Help.xml
Module Name: Firefly.InvokeSqlExecute
online version:
schema: 2.0.0
---

# Invoke-SqlExecute

## SYNOPSIS
Runs a script containing statements supported by the SQL Server SQLCMD utility.

## SYNTAX

### ConnectionString
```
Invoke-SqlExecute [-AbortOnError] -ConnectionString <String> [-ConsoleMessageHandler <ScriptBlock>]
 [-DisableCommands] [-DryRun] [-DisableVariables] [-IncludeSqlUserErrors] [-MaxBinaryLength <Int32>]
 [-MaxCharLength <Int32>] [-InputFile <String[]>] [-OutputAs <OutputAs>] [-OutputFile <String>]
 [-OverrideScriptVariables] [-Parallel] [[-Query] <String>] [-QueryTimeout <Int32>] [-RetryCount <Int32>]
 [-Variable <Object>] [<CommonParameters>]
```

### ConnectionParameters
```
Invoke-SqlExecute [-AbortOnError] [-ConnectionTimeout <Int32>] [-ConsoleMessageHandler <ScriptBlock>]
 [-Database <String>] [-DedicatedAdministratorConnection] [-DisableCommands] [-DryRun] [-DisableVariables]
 [-EncryptConnection] [-IgnoreProviderContext] [-IncludeSqlUserErrors] [-MaxBinaryLength <Int32>]
 [-MaxCharLength <Int32>] [-InputFile <String[]>] [-MultiSubnetFailover] [-OutputAs <OutputAs>]
 [-OutputFile <String>] [-OverrideScriptVariables] [-Parallel] [-Password <String>] [[-Query] <String>]
 [-QueryTimeout <Int32>] [-RetryCount <Int32>] [-ServerInstance <PSObject>] [-SuppressProviderContextWarning]
 [-Username <String>] [-Variable <Object>] [<CommonParameters>]
```

## DESCRIPTION
The Invoke-SqlExecute cmdlet runs a script containing T-SQL and commands supported by the SQL Server SQLCMD utility.
One of the key features of this particular implementation is that it tracks execution through its input, including additional files brought in with :R commands so that if an execution error occurs, it will provide you with a very close location within the input file itself of where the error is, rather than only outputting the SQL server error which only identifies the line number within the currently executing batch.

This cmdlet also accepts many of the commands supported natively by SQLCMD, such as GO and QUIT.

This cmdlet does not support the use of some commands that are primarily related to interactive script editing.
The default Invoke-Sqlcmd cmdlet chooses not to support more of such commands than this implementation.
We deemed it useful to be able to run e.g.
:listvar to dump the current scripting variables to the output channel within a script execution to aid in debugging, and to be able to re-route output and error messages in the middle of a run (:OUT, :ERROR) Those commands that are not supported are ignored if encountered.

The commands not supported include :ed, :perftrace, and :serverlist.

When this cmdlet is run, the first result set that the script returns is displayed as a formatted table.

If subsequent result sets contain different column lists than the first, those result sets are not displayed.

If subsequent result sets after the first set have the same column list, their rows are appended to the formatted table that contains the rows that were returned by the first result set.

You can display SQL Server message output, such as those that result from the SQL PRINT statement by specifying the Verbose parameter.
Additionally, you can capture this output by providing a script block that will receive the message along with its intended destination (StdOut/StdError) and route this data elsewhere.

## EXAMPLES

### EXAMPLE 1
```
PS C:\> Invoke-SqlExecute -Query "SELECT GETDATE() AS TimeOfQuery" -ServerInstance "MyComputer\MyInstance"
```

This is an example of calling Invoke-Sqlcmd to execute a simple query, similar to specifying sqlcmd with the -Q and -S options:

### EXAMPLE 2
```
PS SQLSERVER:\SQL\MyComputer\MyInstance> Invoke-SqlExecute -Query "SELECT @@SERVERNAME AS ServerName"
```

This is an example of calling Invoke-Sqlcmd to execute a simple query, using the provider context for the connection:

## PARAMETERS

### -AbortOnError
Indicates that this cmdlet stops the SQL Server command and returns an error level to the Windows PowerShell LASTEXITCODE variable if this cmdlet encounters an error.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConnectionString
Specifies a connection string to connect to the server.

```yaml
Type: String
Parameter Sets: ConnectionString
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConnectionTimeout
Specifies the number of seconds when this cmdlet times out if it cannot successfully connect to an instance of the Database Engine.
The timeout value must be an integer value between 0 and 65534.
If 0 is specified, connection attempts do not time out.

The default is 8 seconds

```yaml
Type: Int32
Parameter Sets: ConnectionParameters
Aliases:

Required: False
Position: Named
Default value: 8
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConsoleMessageHandler
This is an enhancement over standard Invoke-Sqlcmd behaviour.

For server message output and sqlcmd commands that produce output, this argument specifies a script block that will consume messages that would otherwise go to the console.

The script block is presented with a variable $OutputMessage which has two fields:

- OutputDestination: Either 'StdOut' or 'StdError'
- Message: The message text.

```yaml
Type: ScriptBlock
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Database
Specifies the name of a database.
This cmdlet connects to this database in the instance that is specified in the ServerInstance parameter.

- If the Database parameter is not specified, the database that is used depends on whether the current path specifies both the SQLSERVER:\SQL folder and a database name.
- If the path specifies both the SQL folder and a database name, this cmdlet connects to the database that is specified in the path.
- If the path is not based on the SQL folder, or the path does not contain a database name, this cmdlet connects to the default database for the current login ID.
- If you specify the IgnoreProviderContext parameter switch, this cmdlet does not consider any database specified in the current path, and connects to the database defined as the default for the current login ID.

```yaml
Type: String
Parameter Sets: ConnectionParameters
Aliases: DatabaseName

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DedicatedAdministratorConnection
Indicates that this cmdlet uses a Dedicated Administrator Connection (DAC) to connect to an instance of the Database Engine.

```yaml
Type: SwitchParameter
Parameter Sets: ConnectionParameters
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -DisableCommands
Indicates that this cmdlet turns off some sqlcmd features that might compromise security when run in batch files.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -DryRun
Indicates that a dry run should be performed.
Connections will be made to SQL Server, but no batches will be executed.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -DisableVariables
Indicates that this cmdlet ignores sqlcmd scripting variables.
This is useful when a script contains many INSERT statements that may contain strings that have the same format as variables, such as $(variable_name).

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -EncryptConnection
Indicates that this cmdlet uses Secure Sockets Layer (SSL) encryption for the connection to the instance of the Database Engine specified in the ServerInstance parameter.

```yaml
Type: SwitchParameter
Parameter Sets: ConnectionParameters
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -IgnoreProviderContext
If set, then any connection implied by the current provider context is ignored.

```yaml
Type: SwitchParameter
Parameter Sets: ConnectionParameters
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeSqlUserErrors
In the MS implementation, this parameter forces a DataReader with no returned rows to iterate all available result sets in the batch.
This is the only way an error raised on any statement within the batch other than the first one will raise a SqlException.

This parameter is provided for command line compatibility with Invoke-Sqlcmd, but the execution engine behaves as though it is always set.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxBinaryLength
Limits the amount of binary data that can be returned from binary/image columns.
Default 1024 bytes.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 1024
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxCharLength
Limits the amount of character data that can be returned from binary/image columns.
Default 4000 bytes.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 4000
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputFile
Specifies a file to be used as the query input to this cmdlet.
The file can contain Transact-SQL statements, sqlcmd commands and scripting variables.
Specify the full path to the file.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Path

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MultiSubnetFailover
This is an enhancement over standard SQLCMD behaviour.
If set, enable Multi Subnet Fail-over - required for connection to Always On listeners.

```yaml
Type: SwitchParameter
Parameter Sets: ConnectionParameters
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -OutputAs
Specifies the type of the results this cmdlet outputs.

- DataRows, DataTables and DataSet set the output of the cmdlet to be the corresponding .NET data type.
- Scalar executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
- Text outputs query results to the console or output file with nothing returned in the pipeline as per SQLCMD.EXE.
- None provides no query output of any description and can result in slightly better performance as time is not spent processing result sets. Use this for example when running big database creation or modification scripts.

Possible values: None, Scalar, DataRows, DataSet, DataTables, Text

```yaml
Type: OutputAs
Parameter Sets: (All)
Aliases: TaskAction, As
Accepted values: None, Scalar, DataRows, DataSet, DataTables, Text

Required: False
Position: Named
Default value: DataRows
Accept pipeline input: False
Accept wildcard characters: False
```

### -OutputFile
Redirects stdout messages (e.g.
PRINT, RAISERROR severity \< 10 and sqlcmd command output) to the given file.
This can be changed in script via :OUT

```yaml
Type: String
Parameter Sets: (All)
Aliases: LogFile

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OverrideScriptVariables
This is an enhancement over standard Invoke-sqlcmd behaviour.

If set, this switch prevents any SETVAR commands within the executed script from overriding the values of scripting variables supplied on the command line.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Parallel
This is an enhancement over standard Invoke-sqlcmd behaviour.

If set, and multiple input files or connection strings are specified, then run on multiple threads.
Useful to push the same script to multiple instances simultaneously.

- One connection string, multiple input files: Run all files on this connection. Use :CONNECT in the input files to redirect to other instances.
- Multiple connection strings, one input file or -Query: Run the input against all connections.
- Equal number of connection strings and input files: Run each input against corresponding connection.

It is not possible to send query results to the pipeline in parallel execution mode.
An exception will be thrown if -OutputAs is not None or Text

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Password
Specifies the password for the SQL Server Authentication login ID that was specified in the Username parameter.
Passwords are case-sensitive.
When possible, use Windows Authentication.

```yaml
Type: String
Parameter Sets: ConnectionParameters
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Query
Specifies one or more queries that this cmdlet runs.
The queries can be Transact-SQL or sqlcmd commands.
Multiple queries separated by a semicolon can be specified.

If passing a string literal, do not specify the sqlcmd GO separator.
Escape any double quotation marks included in the string.
Consider using bracketed identifiers such as \[MyTable\] instead of quoted identifiers such as "MyTable".

There are no restrictions if passing a string variable, i.e.
you can read the entire content of a .SQL file into a string variable and provide it here.

```yaml
Type: String
Parameter Sets: (All)
Aliases: Sql

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -QueryTimeout
Specifies the number of seconds before the queries time out.
If a timeout value is not specified, the queries do not time out.
The timeout must be an integer value between 0 and 65535, with 0 meaning infinite.

The default is 0

```yaml
Type: Int32
Parameter Sets: (All)
Aliases: CommandTimeout

Required: False
Position: Named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -RetryCount
This is an enhancement over standard Invoke-Sqlcmd behaviour.

Sets the number of times to retry a failed statement if the error is deemed retryable, e.g.
timeout or deadlock victim.
Errors like key violations are not retryable.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -ServerInstance
Specifies a character string or SQL Server Management Objects (SMO) object that specifies the name of an instance of the Database Engine.
For default instances, only specify the computer name: MyComputer.
For named instances, use the format ComputerName\InstanceName.

```yaml
Type: PSObject
Parameter Sets: ConnectionParameters
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -SuppressProviderContextWarning
Indicates that this cmdlet suppresses the warning that this cmdlet has used in the database context from the current SQLSERVER:\SQL path setting to establish the database context for the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: ConnectionParameters
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Username
Specifies the login ID for making a SQL Server Authentication connection to an instance of the Database Engine.

The password must be specified through the Password parameter.

If Username and Password are not specified, this cmdlet attempts a Windows Authentication connection using the Windows account running the Windows PowerShell session.
When possible, use Windows Authentication.

```yaml
Type: String
Parameter Sets: ConnectionParameters
Aliases: UserId

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Variable
Specifies initial scripting variables for use in the sqlcmd script.

Various data types may be used for the type of this input:

- IDictionary: e.g. a PowerShell hashtable @{ VAR1 = 'Value1'; VAR2 = 'Value 2'}
- string: e.g. "VAR1=value1;VAR2='Value 2'". Note, does not handle semicolons or equals as part of variable's value -use one of the other types
- string array: e.g. @("VAR1=value1", "VAR2=Value 2")

```yaml
Type: Object
Parameter Sets: (All)
Aliases: SqlCmdParameters

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Management.Automation.PSObject
Specifies a character string or SQL Server Management Objects (SMO) object that specifies the name of an instance of the Database Engine.
For default instances, only specify the computer name: MyComputer.
For named instances, use the format ComputerName\InstanceName.

## OUTPUTS

### System.Data.DataRow
### System.Data.DataSet
### System.Data.DataTable
### System.Object
## NOTES

## RELATED LINKS
