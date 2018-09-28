namespace Firefly.InvokeSqlExecute
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    using Firefly.SqlCmdParser;
    using Firefly.SqlCmdParser.Client;

    /// <summary>
    /// <para type="synopsis">Runs a script containing statements supported by the SQL Server SQLCMD utility.</para>
    /// <para type="description">
    /// The Invoke-SqlExecute cmdlet runs a script containing T-SQL and commands supported by the SQL Server SQLCMD utility.
    /// One of the key features of this particular implementation is that it tracks execution through its input, including additional files
    /// brought in with :R commands so that if an execution error occurs, it will provide you with a very close location within the input 
    /// file itself of where the error is, rather than only outputting the SQL server error which only identifies the line number within the currently 
    /// executing batch.
    /// </para>
    /// <para type="description">
    /// This cmdlet also accepts many of the commands supported natively by SQLCMD, such as GO and QUIT.
    /// </para>
    /// <para type="description">
    /// This cmdlet does not support the use of some commands that are primarily related to interactive script editing.
    /// The default Invoke-Sqlcmd cmdlet chooses not to support more of such commands than this implementation. 
    /// We deemed it useful to be able to run e.g. :listvar to dump the current scripting variables to the output channel within a script execution to aid in debugging,
    /// and to be able to re-route output and error messages in the middle of a run (:OUT, :ERROR)
    /// Those commands that are not supported are ignored if encountered.
    /// </para>
    /// <para type="description">
    /// The commands not supported include :ed, :perftrace, and :serverlist.
    /// </para>
    /// <para type="description">
    /// When this cmdlet is run, the first result set that the script returns is displayed as a formatted table.
    /// </para>
    /// <para type="description">
    /// If subsequent result sets contain different column lists than the first, those result sets are not displayed.
    /// </para>
    /// <para type="description">
    /// If subsequent result sets after the first set have the same column list, their rows are appended to the formatted table that contains the rows that were returned by the first result set.
    /// </para>
    /// <para type="description">
    /// You can display SQL Server message output, such as those that result from the SQL PRINT statement by specifying the Verbose parameter.
    /// Additionally, you can capture this output by providing a script block that will receive the message along with its intended destination (StdOut/StdError) and route this data elsewhere.
    /// </para>
    /// </summary>
    /// <example>
    ///   <para>This is an example of calling Invoke-Sqlcmd to execute a simple query, similar to specifying sqlcmd with the -Q and -S options:</para>
    ///   <code>PS C:\> Invoke-SqlExecute -Query "SELECT GETDATE() AS TimeOfQuery" -ServerInstance "MyComputer\MyInstance"</code>
    ///   <para>This is an example of calling Invoke-Sqlcmd to execute a simple query, similar to specifying sqlcmd with the -Q and -S options:</para>
    /// </example>
    /// <example>
    ///   <para>This is an example of calling Invoke-Sqlcmd to execute a simple query, using the provider context for the connection:</para>
    ///   <code>PS SQLSERVER:\SQL\MyComputer\MyInstance> Invoke-SqlExecute -Query "SELECT @@SERVERNAME AS ServerName"</code>
    ///   <para>This is an example of calling Invoke-Sqlcmd to execute a simple query, using the provider context for the connection:</para>
    /// </example>
    /// <seealso cref="T:System.Management.Automation.PSCmdlet" />
    /// <seealso cref="T:Firefly.InvokeSqlExecute.IInvokeSqlExecuteArguments" />
    [Cmdlet(VerbsLifecycle.Invoke, "SqlExecute")]
    [OutputType(typeof(object))]
    [OutputType(typeof(DataRow))]
    [OutputType(typeof(DataTable))]
    [OutputType(typeof(DataSet))]

    // ReSharper disable once InheritdocConsiderUsage
    // ReSharper disable once StyleCop.SA1650
    public class InvokeSqlExecuteCommand : PSCmdlet, ISqlExecuteArguments
    {
        #region Fields

        /// <summary>
        /// Queue of actions to perform on the main thread (interacting with the PowerShell host) when running in parallel mode.
        /// </summary>
        private ConcurrentQueue<MainThreadAction> mainThreadActions = new ConcurrentQueue<MainThreadAction>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeSqlExecuteCommand"/> class.
        /// </summary>
        public InvokeSqlExecuteCommand()
        {
            this.CurrentDirectoryResolver = new PowerShellDirectoryResolver(this);
        }

        #region Cmdlet Parameters

        /// <summary>
        /// Gets or sets the abort on error.
        /// <para type="description">Indicates that this cmdlet stops the SQL Server command and returns an error level to the Windows PowerShell LASTEXITCODE variable if this cmdlet encounters an error.</para>
        /// </summary>
        /// <value>
        /// The abort on error.
        /// </value>
        [Parameter]
        public SwitchParameter AbortOnError { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the connection string.
        /// <para type="description">Specifies a connection string to connect to the server.</para>
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionString", Mandatory = true), ValidateNotNullOrEmpty]
        public string[] ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the login timeout.
        /// <para type="description">
        /// Specifies the number of seconds when this cmdlet times out if it cannot successfully connect to an instance of the Database Engine. 
        /// The timeout value must be an integer value between 0 and 65534. If 0 is specified, connection attempts do not time out.
        /// </para>
        /// <para type="description">The default is 8 seconds</para>
        /// </summary>
        /// <value>
        /// The login timeout.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters"), ValidateRange(0, 65534)]
        public int ConnectionTimeout { get; set; } = 8;

        /// <summary>
        /// Gets or sets the console message handler.
        /// <para type="description">
        /// This is an enhancement over standard Invoke-Sqlcmd behaviour.
        /// </para>
        /// <para type="description">
        /// For server message output and sqlcmd commands that produce output, this argument specifies a script block that will consume messages 
        /// that would otherwise go to the console.
        /// </para>
        /// <para type="description">The script block is presented with a variable $OutputMessage which has these fields:</para>
        /// <para type="description">- OutputDestination: Either 'StdOut' or 'StdError'</para>
        /// <para type="description">- Message: The message text.</para>
        /// <para type="description">- NodeNumber: If running multiple scripts, each gets a unique number. If running in parallel, messages from all nodes will appear as they are raised, i.e. in no particular order.</para>
        /// </summary>
        /// <value>
        /// The console message handler.
        /// </value>
        [Parameter]
        // ReSharper disable once StyleCop.SA1650
        public ScriptBlock ConsoleMessageHandler { get; set; }

        /// <summary>
        /// Gets or sets the database.
        /// <para type="description">
        /// Specifies the name of a database. This cmdlet connects to this database in the instance that is specified in the ServerInstance parameter.
        /// </para>
        /// <para type="description">- If the Database parameter is not specified, the database that is used depends on whether the current path specifies both the SQLSERVER:\SQL folder and a database name.</para>
        /// <para type="description">- If the path specifies both the SQL folder and a database name, this cmdlet connects to the database that is specified in the path.</para>
        /// <para type="description">- If the path is not based on the SQL folder, or the path does not contain a database name, this cmdlet connects to the default database for the current login ID.</para>
        /// <para type="description">- If you specify the IgnoreProviderContext parameter switch, this cmdlet does not consider any database specified in the current path,  and connects to the database defined as the default for the current login ID. </para>
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters"), ValidateNotNullOrEmpty]
        [Alias("DatabaseName")]
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets the dedicated administrator connection.
        /// <para type="description">
        /// Indicates that this cmdlet uses a Dedicated Administrator Connection (DAC) to connect to an instance of the Database Engine.
        /// </para>
        /// </summary>
        /// <value>
        /// The dedicated administrator connection.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters")]
        public SwitchParameter DedicatedAdministratorConnection { get; set; }

        /// <summary>
        /// Gets or sets the disable commands.
        /// <para type="description">
        /// Indicates that this cmdlet turns off some SQLCMD features that might compromise security when run in batch files.
        /// </para>
        /// </summary>
        /// <value>
        /// The disable commands.
        /// </value>
        [Parameter]
        public SwitchParameter DisableCommands { get; set; }

        /// <summary>
        /// Gets or sets the dry run.
        /// <para type="description">
        /// Indicates that a dry run should be performed. Connections will be made to SQL Server, but no batches will be executed.
        /// </para>
        /// </summary>
        /// <value>
        /// The dry run.
        /// </value>
        [Parameter]
        public SwitchParameter DryRun { get; set; }

        /// <summary>
        /// Gets or sets the disable variables.
        /// <para type="description">
        /// Indicates that this cmdlet ignores SQLCMD scripting variables.
        /// This is useful when a script contains many INSERT statements that may contain strings that have the same format as variables, such as $(variable_name).
        /// </para>
        /// </summary>
        /// <value>
        /// The disable variables.
        /// </value>
        [Parameter]
        public SwitchParameter DisableVariables { get; set; }

        /// <summary>
        /// Gets or sets the encrypt connection.
        /// <para type="description">
        /// Indicates that this cmdlet uses Secure Sockets Layer (SSL) encryption for the connection to the instance of the Database Engine specified in the ServerInstance parameter.
        /// </para>
        /// </summary>
        /// <value>
        /// The encrypt connection.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters")]
        public SwitchParameter EncryptConnection { get; set; }

        /// <summary>
        /// Gets or sets the ignore provider context.
        /// <para type="description">
        /// If set, then any connection implied by the current provider context is ignored.
        /// </para>
        /// </summary>
        /// <value>
        /// The ignore provider context.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters")]
        public SwitchParameter IgnoreProviderContext { get; set; }

        /// <summary>
        /// Gets or sets the include SQL user errors.
        /// <para type="description">
        /// In the MS implementation, this parameter forces a DataReader with no returned rows to iterate
        /// all available result sets in the batch. This is the only way an error raised on any statement 
        /// within the batch other than the first one will raise a <see cref="SqlException"/>.
        /// </para>
        /// <para type="description">
        /// This parameter is provided for command line compatibility with Invoke-Sqlcmd, 
        /// but the execution engine behaves as though it is always set.
        /// </para>
        /// </summary>
        /// <value>
        /// The include SQL user errors.
        /// </value>
        // ReSharper disable once UnusedMember.Global
        [Parameter]
        // ReSharper disable once StyleCop.SA1650
        public SwitchParameter IncludeSqlUserErrors { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the maximum length of binary data to return.
        /// <para type="description">
        /// Limits the amount of binary data that can be returned from binary/image columns. Default 1024 bytes.
        /// </para>
        /// </summary>
        /// <value>
        /// The maximum length of the binary.
        /// </value>
        [Parameter]
        [ValidateRange(1, 2147483647)]
        public int MaxBinaryLength { get; set; } = 1024;

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the maximum length of character data to return.
        /// <para type="description">
        /// Limits the amount of character data that can be returned from binary/image columns. Default 4000 bytes.
        /// </para>
        /// </summary>
        /// <value>
        /// The maximum length of the character.
        /// </value>
        [Parameter]
        [ValidateRange(1, 2147483647)]
        public int MaxCharLength { get; set; } = 4000;

        /// <inheritdoc />
        /// <summary>
        /// Gets the input file.
        /// <para type="description">
        /// Specifies a file to be used as the query input to this cmdlet. The file can contain Transact-SQL statements, sqlcmd commands and scripting variables. 
        /// Specify the full path to the file.
        /// </para>
        /// </summary>
        /// <value>
        /// The input file.
        /// </value>
        [Parameter]
        [Alias("Path")]
        public string[] InputFile { get; set; }

        /// <summary>
        /// Gets or sets the multi subnet fail over.
        /// <para type="description">
        /// This is an enhancement over standard SQLCMD behavior.
        /// If set, enable Multi Subnet Fail-over - required for connection to Always On listeners.
        /// </para>
        /// </summary>
        /// <value>
        /// The multi subnet fail-over.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters")]
        public SwitchParameter MultiSubnetFailover { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the results as.
        /// <para type="description">Specifies the type of the results this cmdlet outputs.</para>
        /// <para type="description">- DataRows, DataTables and DataSet set the output of the cmdlet to be the corresponding .NET data type.</para>
        /// <para type="description">- Scalar executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.</para>
        /// <para type="description">- Text outputs query results to the console or output file with nothing returned in the pipeline as per SQLCMD.EXE.</para>
        /// <para type="description">- None provides no query output of any description and can result in slightly better performance as time is not spent processing result sets. Use this for example when running big database creation or modification scripts.</para>
        /// </summary>
        /// <value>
        /// The results as.
        /// </value>
        [Parameter]
        [Alias("TaskAction", "As")]
        public OutputAs OutputAs { get; set; } = OutputAs.DataRows;

        /// <inheritdoc />
        /// <summary>
        /// Gets the output file.
        /// <para type="description">
        /// Redirects stdout messages (e.g. PRINT, RAISERROR severity &lt; 10 and sqlcmd command output) to the given file. This can be changed in script via :OUT
        /// </para>
        /// </summary>
        /// <value>
        /// The output file.
        /// </value>
        [Parameter]
        [Alias("LogFile")]
        public string OutputFile { get; set; }

        /// <summary>
        /// Gets or sets the override script variables.
        /// <para type="description">
        /// This is an enhancement over standard Invoke-sqlcmd behavior.
        /// </para>
        /// <para type="description">
        /// If set, this switch prevents any SETVAR commands within the executed script from overriding the values of scripting variables supplied on the command line.
        /// </para>
        /// </summary>
        /// <value>
        /// The override script variables.
        /// </value>
        [Parameter]
        // ReSharper disable once StyleCop.SA1650
        public SwitchParameter OverrideScriptVariables { get; set; }

        /// <summary>
        /// Gets or sets the parallel.
        /// <para type="description">
        /// This is an enhancement over standard Invoke-sqlcmd behavior.
        /// </para>
        /// <para type="description">
        /// If set, and multiple input files or connection strings are specified, then run on multiple threads.
        /// Useful to push the same script to multiple instances simultaneously.
        /// </para>
        /// <para type="description">- One connection string, multiple input files: Run all files on this connection. Use :CONNECT in the input files to redirect to other instances.</para>
        /// <para type="description">- Multiple connection strings, one input file or -Query: Run the input against all connections.</para>
        /// <para type="description">- Equal number of connection strings and input files: Run each input against corresponding connection.</para>
        /// <para type="description">
        /// It is not possible to send query results to the pipeline in parallel execution mode.
        /// An exception will be thrown if -OutputAs is not None or Text
        /// </para>
        /// </summary>
        /// <value>
        /// The parallel.
        /// </value>
        [Parameter] 
        // ReSharper disable once StyleCop.SA1650
        public SwitchParameter Parallel { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// <para type="description">
        /// Specifies the password for the SQL Server Authentication login ID that was specified in the Username parameter. 
        /// Passwords are case-sensitive. When possible, use Windows Authentication.
        /// </para>
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters"), ValidateNotNullOrEmpty]
        public string Password { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the query.
        /// <para type="description">
        /// Specifies one or more queries that this cmdlet runs. The queries can be Transact-SQL or sqlcmd commands. Multiple queries separated by a semicolon can be specified. 
        /// </para>
        /// <para type="description">
        /// If passing a string literal, do not specify the sqlcmd GO separator. Escape any double quotation marks included in the string. Consider using bracketed identifiers such as [MyTable] instead of quoted identifiers such as "MyTable". 
        /// </para>
        /// <para type="description">
        /// There are no restrictions if passing a string variable, i.e. you can read the entire content of a .SQL file into a string variable and provide it here.
        /// </para>
        /// </summary>
        /// <value>
        /// The query.
        /// </value>
        [Parameter(Position = 0), ValidateNotNullOrEmpty]
        [Alias("Sql")]
        public string Query { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the query timeout.
        /// <para type="description">
        /// Specifies the number of seconds before the queries time out. If a timeout value is not specified, the queries do not time out. The timeout must be an integer value between 0 and 65535, with 0 meaning infinite.
        /// </para>
        /// <para type="description">The default is 0</para>
        /// </summary>
        /// <value>
        /// The query timeout.
        /// </value>
        [Parameter, ValidateRange(0, 65535)]
        [Alias("CommandTimeout")]
        public int QueryTimeout { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the number of times to retry a retryable error (e.g. timed-out queries).
        /// <para type="description">
        /// This is an enhancement over standard Invoke-Sqlcmd behaviour.
        /// </para>
        /// <para type="description">
        /// Sets the number of times to retry a failed statement if the error is deemed retryable, e.g. timeout or deadlock victim. Errors like key violations are not retryable. 
        /// </para>
        /// </summary>
        /// <value>
        /// The retry count.
        /// </value>
        [Parameter, ValidateRange(0, 65535)]
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the server.
        /// <para type="description">
        /// Specifies a character string or SQL Server Management Objects (SMO) object that specifies the name of an instance of the Database Engine. 
        /// For default instances, only specify the computer name: MyComputer. For named instances, use the format ComputerName\InstanceName.
        /// </para>
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters", ValueFromPipeline = true)]
        public PSObject ServerInstance { get; set; }

        /// <summary>
        /// Gets or sets the suppress provider context warning.
        /// <para type="description">Indicates that this cmdlet suppresses the warning that this cmdlet has used in the database context from the current SQLSERVER:\SQL path setting to establish the database context for the cmdlet.</para>
        /// </summary>
        /// <value>
        /// The suppress provider context warning.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters")]
        public SwitchParameter SuppressProviderContextWarning { get; set; }

        /// <summary>
        /// Gets or sets the user name.
        /// <para type="description">Specifies the login ID for making a SQL Server Authentication connection to an instance of the Database Engine.</para>
        /// <para type="description">The password must be specified through the Password parameter.</para>
        /// <para type="description">
        /// If Username and Password are not specified, this cmdlet attempts a Windows Authentication connection using the Windows account running the Windows PowerShell session.
        /// When possible, use Windows Authentication.
        /// </para>
        /// </summary>
        /// <value>
        /// The user identifier.
        /// </value>
        [Parameter(ParameterSetName = "ConnectionParameters"), ValidateNotNullOrEmpty]
        [Alias("UserId")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the variable.
        /// <para type="description">
        /// Specifies initial scripting variables for use in the SQLCMD script.
        /// </para>
        /// <para type="description">
        /// Various data types may be used for the type of this input:
        /// </para>
        /// <para type="description">- IDictionary: e.g. a PowerShell hashtable @{ VAR1 = 'Value1'; VAR2 = 'Value 2'}</para>
        /// <para type="description">- string: e.g. "VAR1=value1;VAR2='Value 2'". Note, does not handle semicolons or equals as part of variable's value -use one of the other types</para>
        /// <para type="description">- string array: e.g. @("VAR1=value1", "VAR2=Value 2")</para>
        /// </summary>
        /// <value>
        /// The variable.
        /// </value>
        [Parameter]
        [Alias("SqlCmdParameters")]
        // ReSharper disable once StyleCop.SA1650
        public object Variable { get; set; }

        #endregion

        #region Other properties

        /// <inheritdoc />
        /// <summary>
        /// Gets a value indicating whether [abort on error].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [abort on error]; otherwise, <c>false</c>.
        /// </value>
        public bool AbortOnErrorSet => this.AbortOnError;

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the connected event handler.
        /// </summary>
        public EventHandler<ConnectEventArgs> Connected { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the current directory resolver.
        /// </summary>
        /// <value>
        /// The current directory resolver. If <c>null</c> then <see cref="M:System.IO.Directory.GetCurrentDirectory" /> is used.
        /// </value>
        public ICurrentDirectoryResolver CurrentDirectoryResolver { get; }

        /// <inheritdoc />
        /// <summary>
        /// Gets a value indicating whether to disable interactive commands, startup script, and environment variables.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [disable commands]; otherwise, <c>false</c>.
        /// </value>
        public bool DisableCommandsSet => this.DisableCommands;

        /// <summary>
        /// Gets a value indicating whether parse only (no execute) run should be performed.
        /// </summary>
        /// <value>
        /// <c>true</c> if [parse only]; otherwise, <c>false</c>.
        /// </value>
        public bool ParseOnly => this.DryRun;

        /// <inheritdoc />
        /// <summary>
        /// Gets a value indicating whether [disable variables].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [disable variables]; otherwise, <c>false</c>.
        /// </value>
        public bool DisableVariablesSet => this.DisableVariables;

        /// <inheritdoc />
        /// <summary>
        /// Gets the initial variables.
        /// </summary>
        /// <value>
        /// The initial variables.
        /// </value>
        /// <exception cref="T:System.FormatException">Syntax error in -Variable value</exception>
        /// <exception cref="T:System.InvalidCastException"></exception>
        public IDictionary InitialVariables
        {
            get
            {
                // ReSharper disable StyleCop.SA1126
                switch (this.Variable)
                {
                    case null:

                        return new Dictionary<string, string>();

                    case IDictionary _:

                        // e.g. PowerShell hashtable
                        return (IDictionary)this.Variable;

                    case string[] _:

                        // Array of variable=value
                        return GetCommandLineVariables((string[])this.Variable);

                    case object[] objArray:

                        // Array of variable=value.
                        // Cast to string array first.
                        return GetCommandLineVariables(objArray.Select(CastVariableDeclarationToString).ToArray());

                    case string _:

                        // Split key/value pairs (name=value;name=value ... )
                        // Note - does NOT handle semicolons in variable values - use a string array or hashtable instead.
                        return GetCommandLineVariables(
                            ((string)this.Variable).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

                    default:

                        throw new InvalidCastException(
                            $"Cannot derive variables from type ${this.Variable.GetType().FullName}");
                }
                // ReSharper restore StyleCop.SA1126
            }
        }

        /// <summary>
        /// Gets or sets the output message.
        /// </summary>
        /// <value>
        /// The output message.
        /// </value>
        public EventHandler<OutputMessageEventArgs> OutputMessage { get; set; }

        /// <summary>
        /// Gets or sets the pipeline result event handler.
        /// </summary>
        /// <value>
        /// The output result.
        /// </value>
        public EventHandler<OutputResultEventArgs> OutputResult { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets a value indicating whether [override script variables].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [override script variables]; otherwise, <c>false</c>.
        /// </value>
        public bool OverrideScriptVariablesSet => this.OverrideScriptVariables;

        /// <summary>
        /// Gets a value indicating whether to run multiple connections/input files in parallel.
        /// </summary>
        /// <value>
        /// <c>true</c> if [run parallel]; otherwise, <c>false</c>.
        /// </value>
        public bool RunParallel => this.Parallel;

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the exit code.
        /// </summary>
        /// <value>
        /// The exit code.
        /// </value>
        public int ExitCode { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="InvokeSqlExecuteCommand"/> is verbose.
        /// </summary>
        /// <value>
        ///   <c>true</c> if verbose; otherwise, <c>false</c>.
        /// </value>
        private bool Verbose =>
            this.MyInvocation.BoundParameters.ContainsKey("Verbose")
            && ((SwitchParameter)this.MyInvocation.BoundParameters["Verbose"]).ToBool();

        /// <summary>
        /// Gets a value indicating whether this <see cref="InvokeSqlExecuteCommand"/> is debug.
        /// </summary>
        /// <value>
        ///   <c>true</c> if debug; otherwise, <c>false</c>.
        /// </value>
        private bool Debug => this.MyInvocation.BoundParameters.ContainsKey("Debug")
                              && ((SwitchParameter)this.MyInvocation.BoundParameters["Debug"]).ToBool();

        #endregion

        /// <summary>
        /// Begins the processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            this.WriteDebugMessage("Argument info");
            this.WriteDebugMessage($"ConnectionString: {ArgumentHelpers.DescribeArgument(this.ConnectionString)}");
            this.WriteDebugMessage($"InputFile:        {ArgumentHelpers.DescribeArgument(this.InputFile)}");
            this.WriteDebugMessage($"Query:            {ArgumentHelpers.DescribeArgument(this.Query)}");
            this.WriteDebugMessage($"Parallel:         {ArgumentHelpers.DescribeArgument(this.Parallel.ToBool())}");

            // Do some argument checks
            if (!(ArgumentHelpers.IsEmptyArgument(this.InputFile) ^ ArgumentHelpers.IsEmptyArgument(this.Query)))
            {
                throw new ArgumentException("Must specify either -Query or -InputFile, bur not both or neither.");
            }

            if (!(ArgumentHelpers.IsEmptyArgument(this.ConnectionString)
                  || ArgumentHelpers.IsEmptyArgument(this.InputFile))
                && this.ConnectionString.Length != this.InputFile.Length && this.ConnectionString.Length > 1
                && this.InputFile.Length > 1)
            {
                // Must be 1 connection, many inputs or 1 input, many connections
                throw new ArgumentException(
                    "Number of connection strings does not match number of input files. Must be 1 connection many files, or 1 file many connections, or an equal number of both.");
            }

            if (ArgumentHelpers.IsSingleRunConfiguration(this.ConnectionString, this.InputFile))
            {
                // Override -Parallel argument for single run configuration.
                this.Parallel = false;
            }

            if (this.RunParallel && !(this.OutputAs == OutputAs.None || this.OutputAs == OutputAs.Text))
            {
                this.WriteWarning("Cannot send results to pipeline in parallel execution mode. Query output will be sent as text to the console.");
                this.OutputAs = OutputAs.Text;
            }

            this.OutputMessage = this.OnOutputMessage;
            this.OutputResult = this.OnOutputResult;
            this.Connected = this.OnConnect;

            if (this.ParameterSetName == "ConnectionParameters")
            {
                this.ConnectionString = this.BuildConnectionString();
            }
        }

        /// <summary>
        /// Processes the record.
        /// </summary>
        protected override void ProcessRecord()
        {
            using (var sqlcmd = new SqlExecuteImpl(this))
            {
                try
                {
                    var task = sqlcmd.Execute();

                    if (task != null)
                    {
                        // Parallel execution. Loop until tasks complete whilst looking for stuff that needs to be processed on this thread.
                        while (!task.Wait(100) || this.mainThreadActions.Count > 0)
                        {
                            if (this.mainThreadActions.TryDequeue(out var action))
                            {
                                try
                                {
                                    action.Action();
                                }
                                finally
                                {
                                    action.CompletionToken.Set();
                                }
                            }
                        }
                    }

                    if (sqlcmd.ErrorCount > 0)
                    {
                        // If -AbortOnError wasn't set, nothing will be thrown from within Execute() method
                        // so throw something here.
                        throw new ScriptExecutionException(sqlcmd.SqlExceptions);
                    }

                    // Set ERRORLEVEL from script
                    this.AssignExitCode(sqlcmd.GetErrorLevel());
                }
                catch (SqlException e)
                {
                    // -AbortOnError was set, or the initial connect failed
                    this.OnOutputMessage(this, new OutputMessageEventArgs(0, e.Format(), OutputDestination.StdError));
                    this.AssignExitCode(1);
                    throw new ScriptExecutionException(e);
                }
                catch (Exception e)
                {
                    this.AssignExitCode(1);
#if DEBUG
                    Console.WriteLine(e);
#endif
                    throw;
                }
                finally
                {
                    this.SessionState.PSVariable.Set("global:LASTEXITCODE", this.ExitCode);
                }
            }
        }

        /// <summary>
        /// Casts a variable declaration to a string.
        /// </summary>
        /// <param name="objectValue">The object value.</param>
        /// <returns>String value of the input.</returns>
        /// <exception cref="InvalidCastException">The element was not a string.</exception>
        private static string CastVariableDeclarationToString(object objectValue)
        {
            if (objectValue is string s)
            {
                // ReSharper disable once StyleCop.SA1126
                return s;
            }

            throw new InvalidCastException(
                $"Cannot derive variables from type ${objectValue.GetType().FullName}");
        }

        /// <summary>
        /// Gets the command line variables if specified as strings.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <returns>Dictionary of variables</returns>
        /// <exception cref="FormatException">Syntax error in -Variable value</exception>
        private static IDictionary GetCommandLineVariables(string[] variables)
        {
            var dict = new Dictionary<string, string>();

            foreach (var variable in variables)
            {
                var ind = variable.IndexOf("=", StringComparison.Ordinal);

                if (ind < 0)
                {
                    throw new FormatException("Syntax error in -Variable value");
                }

                dict.Add(variable.Substring(0, ind).Trim('\t', ' ', '\'', '"'), variable.Substring(ind + 1).Trim('\t', ' ', '\'', '"'));
            }

            return dict;
        }

        /// <summary>
        /// Assign exit code if nothing else has already set it.
        /// </summary>
        /// <param name="code">The code to assign.</param>
        private void AssignExitCode(int code)
        {
            if (this.ExitCode == 0)
            {
                this.ExitCode = code;
            }
        }

        /// <summary>
        /// Builds a connection string out of whatever parameter combination was supplied.
        /// </summary>
        /// <returns>The connection string.</returns>
        private string[] BuildConnectionString()
        {
            var providerPath = this.SessionState.Path.CurrentLocation.ProviderPath;
            dynamic serverConnection = null;
            string database = null;

            if (!this.IgnoreProviderContext && providerPath.StartsWith(
                    @"SQLSERVER:\SQL",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Get the current SQL server provider location
                var psobject = this.InvokeCommand.InvokeScript("Get-Item .").FirstOrDefault();

                if (psobject != null)
                {
                    // Use reflection to do this to make the code SMO version independent.
                    var smoObject = psobject.Is("Microsoft.SqlServer.Management.Sdk.Sfc.SfcCollectionInfo")
                                        ? ((dynamic)psobject.BaseObject).Collection
                                        : psobject.BaseObject;

                    while (true)
                    {
                        // Walk back up the provider path discovering potentially a database and server context
                        var type = (Type)smoObject.GetType();
                        var interfaces = type.GetInterfaces();
                        var typeName = type.Name;

                        if (typeName == "Server")
                        {
                            serverConnection = smoObject.ConnectionContext;
                            break;
                        }

                        if (typeName == "Database")
                        {
                            database = smoObject.Name;
                        }

                        if (interfaces.Any(i => i.Name == "ISfcDomain"))
                        {
                            // If we get here, we are outside of any server context
                            break;
                        }

                        // Get parent object to walk up the tree.
                        var prop = (PropertyInfo)smoObject.GetType().GetProperty("Parent");

                        if (prop == null)
                        {
                            // We've reached the root of the SMO namespace
                            break;
                        }

                        smoObject = prop.GetValue(smoObject, null);
                    }
                }

                if (serverConnection != null)
                {
                    if (this.Database == null && database != null)
                    {
                        this.Database = database;

                        if (!this.SuppressProviderContextWarning)
                        {
                            this.WriteWarning(
                                $"Using provider context. Server = {serverConnection.ServerInstance}, Database = {database}");
                        }
                    }
                    else if (!this.SuppressProviderContextWarning)
                    {
                        this.WriteWarning($"Using provider context. Server = {serverConnection.ServerInstance}");
                    }

                    if (this.Username == null && !serverConnection.LoginSecure)
                    {
                        this.Username = serverConnection.Login;
                        this.Password = serverConnection.Password;
                    }

                    if (serverConnection.LoginSecure)
                    {
                        this.Username = null;
                    }

                    if (this.EncryptConnection == false)
                    {
                        this.EncryptConnection = serverConnection.EncryptConnection;
                    }
                }
            }

            string instance = (serverConnection == null) ? "." : serverConnection.ServerInstance;

            // ReSharper disable NotResolvedInText
            if (this.ServerInstance != null)
            {
                if (this.ServerInstance.BaseObject is string s)
                {
                    // ReSharper disable once StyleCop.SA1126
                    instance = s;
                }
                else if (this.ServerInstance.Is("Microsoft.SqlServer.Management.Smo.Server"))
                {
                    instance = ((dynamic)this.ServerInstance.BaseObject).Name;
                }
                else
                {
                    this.ThrowTerminatingError(
                        new ErrorRecord(
                            new ArgumentNullException("ServerInstance"),
                            "CannotGetServerInstance",
                            ErrorCategory.InvalidArgument,
                            null));
                }
            }

            if (string.IsNullOrEmpty(instance))
            {
                this.ThrowTerminatingError(
                    new ErrorRecord(
                        new ArgumentNullException("ServerInstance"),
                        "CannotGetServerInstance",
                        ErrorCategory.InvalidArgument,
                        null));
            }

            // ReSharper restore NotResolvedInText
            var connectionStringBuilder = new DbConnectionStringBuilder
                                              {
                                                  {
                                                      "Server",
                                                      (this.DedicatedAdministratorConnection
                                                           ? "admin:"
                                                           : string.Empty) + instance
                                                  },
                                                  {
                                                      "Application Name",
                                                      Path.GetFileNameWithoutExtension(
                                                          Assembly.GetExecutingAssembly()
                                                              .Location)
                                                  },
                                                  { "Workstation Id", Environment.MachineName }
                                              };

            if (this.Database != null)
            {
                connectionStringBuilder.Add("Database", this.Database);
            }

            if (this.Username != null)
            {
                connectionStringBuilder.Add("Uid", this.Username);
                connectionStringBuilder.Add("Pwd", this.Password ?? string.Empty);
            }
            else
            {
                connectionStringBuilder.Add("Trusted_Connection", "yes");
            }

            if (this.ConnectionTimeout != 0)
            {
                connectionStringBuilder.Add("Connection Timeout", this.ConnectionTimeout.ToString());
            }

            if (this.EncryptConnection)
            {
                connectionStringBuilder.Add("Encrypt", "yes");
            }

            if (this.MultiSubnetFailover)
            {
                connectionStringBuilder.Add("MultiSubnetFailover", "yes");
            }

            return new[] { connectionStringBuilder.ConnectionString };
        }

        /// <summary>
        /// Called to output a message to the console (e.g. PRINT, RAISERROR, info messages from commands etc.).
        /// File redirection is handled within the <c>SqlCmdParser</c> assembly.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="OutputMessageEventArgs"/> instance containing the event data.</param>
        private void OnOutputMessage(object sender, OutputMessageEventArgs args)
        {
            if (this.ConsoleMessageHandler != null)
            {
                // User supplied a scriptblock with -ConsoleMessageHandler, then execute it.
                var action = new Action(
                    () =>
                        {
                            this.ConsoleMessageHandler.InvokeWithContext(
                                null,
                                new List<PSVariable>
                                    {
                                        new PSVariable(
                                            "OutputMessage",
                                            args,
                                            ScopedItemOptions.Constant)
                                    },
                                null);
                        });

                if (this.RunParallel)
                {
                    // Must be run on main thread.
                    var completionToken = new ManualResetEvent(false);
                    var qa = new MainThreadAction { Action = action };
                    this.mainThreadActions.Enqueue(new MainThreadAction { Action = action, CompletionToken = completionToken});
                    completionToken.WaitOne();
                }
                else
                {
                    action();
                }
            }
            else
            {
                string message;

                if (this.Parallel)
                {
                    // Reformat the message to indicate execution node number
                    var lines = new List<string>();
                    var firstLine = true;

                    foreach (var line in args.Message.TrimEnd(Environment.NewLine.ToCharArray()).Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    {
                        if (firstLine)
                        {
                            lines.Add($"{args.NodeNumber:D2}> {line}");
                            firstLine = false;
                        }
                        else
                        {
                            lines.Add($"    {line}");
                        }
                    }

                    message = string.Join(Environment.NewLine, lines);
                }
                else
                {
                    message = args.Message;
                }

                switch (args.OutputDestination)
                {
                    case OutputDestination.StdOut:

                        if (this.Verbose)
                        {
                            if (this.Host.Name == "ConsoleHost")
                            {
                                Console.WriteLine(message);
                            }
                            else
                            {
                                this.Host.UI.WriteLine(message);
                            }
                        }

                        break;

                    case OutputDestination.StdError:

                        if (this.Host.Name == "ConsoleHost")
                        {
                            var c = Console.ForegroundColor;

                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.WriteLine(message);
                            Console.ForegroundColor = c;
                        }
                        else
                        {
                            this.Host.UI.WriteErrorLine(message);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="OutputResult"/> is raised to send a result set to the pipeline,
        /// or the console output stream for -As Text
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="OutputResultEventArgs"/> instance containing the event data.</param>
        private void OnOutputResult(object sender, OutputResultEventArgs args)
        {
            if (this.OutputAs == OutputAs.Text)
            {
                string formattedTable;
                var dt = (DataTable)args.Result;

                // Get PowerShell to format the result DataTable to a text table and harvest that text.
                // Need to create a runspace in case we are executing in parallel.
                using (var runspace = RunspaceFactory.CreateRunspace())
                {
                    runspace.Open();

                    using (var ps = PowerShell.Create().AddScript("param($dt) $dt | Format-Table | Out-String"))
                    {
                        ps.Runspace = runspace;
                        ps.AddArgument(dt);

                        formattedTable = string.Join(Environment.NewLine, ps.Invoke<string>());
                    }
                }

                // Generate a rows affected message.
                var rows = dt.Rows.Count == 1 ? "row" : "rows";
                var rowsaffected = $"({dt.Rows.Count} {rows} affected)\n";

                switch (args.OutputDestination)
                {
                    case OutputDestination.StdOut:

                        this.OnOutputMessage(sender, new OutputMessageEventArgs(args.NodeNumber, formattedTable + rowsaffected, OutputDestination.StdOut));
                        break;

                    case OutputDestination.File:

                        if (args.OutputStream == null)
                        {
#if DEBUG
                            Console.WriteLine("Attempt to write output to file, but stream is null");
#endif
                        }
                        else
                        {
                            var bytes = Encoding.UTF8.GetBytes(formattedTable + rowsaffected);
                            args.OutputStream.Write(bytes, 0, bytes.Length);
                        }

                        break;
                }
            }
            else
            {
                this.WriteObject(args.Result);
            }
        }

        /// <summary>
        /// Called when [connect].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ConnectEventArgs"/> instance containing the event data.</param>
        private void OnConnect(object sender, ConnectEventArgs args)
        {
        }

        /// <summary>
        /// Writes a debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <remarks>
        /// Using this method as I don't seem to be able to change $DebugPreference from within the cmdlet,
        /// so we end up getting nasty confirmation prompts.
        /// </remarks>
        private void WriteDebugMessage(string message)
        {
            if (!this.Debug)
            {
                return;
            }

            var fg = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"DEBUG: {message}");
            Console.ForegroundColor = fg;
        }

        /// <inheritdoc />
        /// <summary>
        /// Concrete <see cref="T:Firefly.SqlCmdParser.ICurrentDirectoryResolver" /> to return the file system path as PowerShell sees it.
        /// </summary>
        /// <seealso cref="T:Firefly.SqlCmdParser.ICurrentDirectoryResolver" />
        private class PowerShellDirectoryResolver : ICurrentDirectoryResolver
        {
            /// <summary>
            /// The cmdlet
            /// </summary>
            private readonly PSCmdlet cmdlet;

            /// <summary>
            /// Initializes a new instance of the <see cref="PowerShellDirectoryResolver"/> class.
            /// </summary>
            /// <param name="cmdlet">The cmdlet.</param>
            public PowerShellDirectoryResolver(PSCmdlet cmdlet)
            {
                this.cmdlet = cmdlet;
            }

            /// <inheritdoc />
            /// <summary>
            /// Gets the current directory.
            /// </summary>
            /// <returns>The current PowerShell file system path.</returns>
            public string GetCurrentDirectory()
            {
                return this.cmdlet.SessionState.Path.CurrentFileSystemLocation.Path;
            }
        }

        /// <summary>
        /// Wrapper for an action to be performed on the main thread.
        /// </summary>
        private class MainThreadAction
        {
            /// <summary>
            /// Gets or sets the action.
            /// </summary>
            /// <value>
            /// The action.
            /// </value>
            public Action Action { get; set; }

            /// <summary>
            /// Gets or sets the completion token.
            /// </summary>
            /// <value>
            /// The completion token.
            /// </value>
            public ManualResetEvent CompletionToken { get; set; }
        }
    }
}