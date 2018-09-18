// ReSharper disable InheritdocConsiderUsage
namespace Firefly.SqlCmdParser.Client
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;

    // ReSharper disable once CommentTypo

    /// <summary>
    /// Concrete command executer with virtual methods permitting creation of custom executers with some behaviour redefined
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    public class CommandExecuter : ICommandExecuter
    {
        /// <summary>
        /// SQL server or provider error codes that represent failures that can be retried.
        /// </summary>
        private static readonly int[] RetryableErrors = new[]
                                                            {
                                                                -2, // ADO.NET timeout
                                                                11, // General network error
                                                                1205 // Deadlock victim 
                                                            };

        /// <summary>
        /// The arguments
        /// </summary>
        private readonly ISqlExecuteArguments arguments;

        /// <summary>
        /// The results as
        /// </summary>
        private readonly OutputAs resultsAs;

        /// <summary>
        /// The variable resolver
        /// </summary>
        private readonly IVariableResolver variableResolver;

        /// <summary>
        /// The connection
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// The stderr destination
        /// </summary>
        private OutputDestination stderrDestination = OutputDestination.StdError;

        /// <summary>
        /// The stderr file
        /// </summary>
        private Stream stderrFile;

        /// <summary>
        /// The stdout destination
        /// </summary>
        private OutputDestination stdoutDestination = OutputDestination.StdOut;

        /// <summary>
        /// The stdout file
        /// </summary>
        private Stream stdoutFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandExecuter" /> class.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        /// <param name="variableResolver">The variable resolver.</param>
        public CommandExecuter(ISqlExecuteArguments arguments, IVariableResolver variableResolver)
        {
            this.resultsAs = arguments.OutputAs;
            this.arguments = arguments;
            this.variableResolver = variableResolver;
        }

        /// <summary>
        /// Occurs when a database connection is made
        /// </summary>
        public event EventHandler<ConnectEventArgs> Connected;

        /// <summary>
        /// Occurs when a message is ready.
        /// </summary>
        public event EventHandler<OutputMessageEventArgs> Message;

        /// <summary>
        /// Occurs when a result or result set is ready.
        /// </summary>
        public event EventHandler<OutputResultEventArgs> Result;

        /// <summary>
        /// Gets the custom exit code set by :EXIT(query).
        /// </summary>
        /// <value>
        /// The custom exit code. If <c>null</c> then :EXIT was not encountered.
        /// </value>
        public int? CustomExitCode { get; private set; }

        /// <summary>
        /// Gets or sets the error action.
        /// </summary>
        /// <value>
        /// The error action.
        /// </value>
        public ErrorAction ErrorAction { get; set; } = ErrorAction.Ignore;

        /// <summary>
        /// Gets or sets the number of <see cref="SqlException"/> errors recorded by <see cref="ProcessBatch"/>.
        /// Retryable errors that retried and then successfully executed are not counted.
        /// </summary>
        /// <value>
        /// The error count.
        /// </value>
        public int ErrorCount => this.SqlExceptions.Count;

        /// <summary>
        /// Gets or sets the list of SQL exceptions thrown during the batch execution.
        /// </summary>
        /// <value>
        /// The SQL exceptions.
        /// </value>
        public IList<SqlException> SqlExceptions { get; protected set; } = new List<SqlException>();

        /// <inheritdoc />
        /// <summary>
        /// <c>:CONNECT</c> directive.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="server">The server.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        public virtual void Connect(int timeout, string server, string user, string password)
        {
            if (this.connection != null)
            {
                this.connection.InfoMessage -= this.OnSqlInfoMessageEvent;
            }

            var cb = new SqlConnectionStringBuilder
                         {
                             DataSource = server,
                             ConnectTimeout = timeout,
                             WorkstationID = Environment.MachineName
                         };

            if (!string.IsNullOrEmpty(user))
            {
                cb.UserID = user;
                cb.Password = password;
            }
            else
            {
                cb.IntegratedSecurity = true;
            }

            this.DoConnect(cb);
        }

        /// <summary>
        /// Connects the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public void ConnectWithConnectionString(string connectionString)
        {
            this.DoConnect(new SqlConnectionStringBuilder(connectionString));
        }

        /// <inheritdoc />
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.connection?.Close();
            this.connection?.Dispose();
            this.stdoutFile?.Dispose();
            this.stderrFile?.Dispose();
        }

        /// <summary>
        ///   <c>:ED</c> directive
        /// </summary>
        /// <param name="batch">The current batch.</param>
        /// <returns>
        /// <returns>The edited batch as a new <see cref="IBatchSource"/>; or <c>null</c> if no changes were made.</returns>
        /// </returns>
        /// <inheritdoc /> 
        public virtual IBatchSource Ed(string batch)
        {
            // Default behaviour, no edit
            return null;
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>:ERROR</c> directive
        /// </summary>
        /// <param name="od">The od.</param>
        /// <param name="fileName">Name of the file.</param>
        public virtual void Error(OutputDestination od, string fileName)
        {
            if (od == OutputDestination.File)
            {
                if (!TryCreateOutputFile(fileName, out var stream))
                {
                    // If the file is not available because of permissions or other reasons, the output will not be switched and will be sent to the last specified or default destination.
                    return;
                }

                this.stderrDestination = od;
                this.stderrFile = stream;
            }
            else
            {
                this.stderrDestination = od;

                if (this.stderrFile == null)
                {
                    return;
                }

                this.stderrFile.Dispose();
                this.stderrFile = null;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>!!</c> directive.
        /// </summary>
        /// <param name="command">The command.</param>
        public virtual void ExecuteShellCommand(string command)
        {
            if (this.arguments.DisableCommandsSet)
            {
                this.WriteStdoutMessage($"[{command}] - Ignored due to DisableCommands flag being set.");
                return;
            }

            if (this.arguments.ParseOnly)
            {
                return;
            }

            // Buffer the output written by the external process. 
            // Attempting to write this to the PowerShell host UI from within the OutputDataReceived/ErrorDataReceived events crashes PowerShell.
            var outputData = new List<ShellExecuteOutput>();

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                                        {
                                            FileName = "cmd.exe",
                                            Arguments = $"/c {command}",
                                            CreateNoWindow = true,
                                            RedirectStandardError = true,
                                            RedirectStandardOutput = true,
                                            UseShellExecute = false,
                                        };

                process.EnableRaisingEvents = true;

                process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            outputData.Add(new ShellExecuteOutput { OutputDestination = OutputDestination.StdOut, Data = e.Data });
                        }
                    };
                process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            outputData.Add(new ShellExecuteOutput { OutputDestination = OutputDestination.StdError, Data = e.Data });
                        }
                    };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                foreach (var output in outputData)
                {
                    if (output.OutputDestination == OutputDestination.StdOut)
                    {
                        this.WriteStdoutMessage(output.Data);
                    }
                    else
                    {
                        this.WriteStderrMessage(output.Data);
                    }
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>:EXIT</c> directive
        /// </summary>
        /// <param name="batch">The batch.</param>
        /// <param name="exitBatch">The exit batch.</param>
        public virtual void Exit(SqlBatch batch, string exitBatch)
        {
            this.ProcessBatch(batch, 1);

            if (string.IsNullOrWhiteSpace(exitBatch) || this.arguments.ParseOnly)
            {
                return;
            }

            using (var command = this.connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = exitBatch;

                try
                {
                    this.CustomExitCode = (int)command.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    // Ignore any error: "When an incorrect query is specified, sqlcmd will exit without a return value."
                    // Error here may include InvalidCastException if the scalar result was null or not an integer.
                    this.WriteStdoutMessage($"Warning: Error in EXIT(query): {ex.Message}");
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>:HELP</c> directive
        /// </summary>
        public virtual void Help()
        {
        }

        /// <summary>
        ///   <c>:R</c> directive.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>
        /// New <see cref="IBatchSource" /> to read from.
        /// </returns>
        /// <exception cref="FileNotFoundException">Include file not found</exception>
        /// <inheritdoc />
        public virtual IBatchSource IncludeFileName(string fileName)
        {
            if (File.Exists(fileName))
            {
                return new BatchSourceFile(fileName);
            }

            throw new FileNotFoundException("Include file not found", fileName);
        }

        /// <summary>
        ///   <c>:LIST</c> directive.
        /// </summary>
        /// <param name="batch">The batch.</param>
        /// <remarks>
        /// Implementations may do additional formatting.
        /// </remarks>
        /// <inheritdoc />
        public virtual void List(string batch)
        {
            this.WriteStdoutMessage(batch);
        }

        /// <summary>
        ///   <c>:LISTVAR</c> directive.
        /// </summary>
        /// <param name="varList">The variable list.</param>
        /// <remarks>
        /// Implementation may decide to redact passwords etc.
        /// </remarks>
        /// <inheritdoc />
        public virtual void ListVar(IDictionary<string, string> varList)
        {
            var output = new StringBuilder();
            output.AppendLine("Current state of scripting variables:");

            foreach (var kv in varList.OrderBy(kv => kv.Key))
            {
                var val = kv.Value;

                if (kv.Key.ToUpperInvariant().Contains("PASSWORD"))
                {
                    // Redact anything that may be a password
                    val = "** REDACTED **";
                }

                if (val == string.Empty)
                {
                    val = "<EMPTY STRING>";
                }

                if (val == null)
                {
                    val = "<NULL>";
                }

                output.AppendLine($"  {kv.Key} = \"{val}\"");
            }

            this.WriteStdoutMessage(output.ToString().TrimEnd('\r', '\n'));
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>:ON ERROR</c> directive.
        /// </summary>
        /// <param name="ea">The ea.</param>
        public virtual void OnError(ErrorAction ea)
        {
            this.ErrorAction = ea;
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>:OUT</c> directive.
        /// </summary>
        /// <param name="od">The od.</param>
        /// <param name="fileName">Name of the file.</param>
        public virtual void Out(OutputDestination od, string fileName)
        {
            if (od == OutputDestination.File)
            {
                if (!TryCreateOutputFile(fileName, out var stream))
                {
                    // If the file is not available because of permissions or other reasons, the output will not be switched and will be sent to the last specified or default destination.
                    return;
                }

                this.stdoutDestination = od;
                this.stdoutFile = stream;
            }
            else
            {
                this.stdoutDestination = od;

                if (this.stdoutFile != null)
                {
                    this.stdoutFile.Dispose();
                    this.stdoutFile = null;
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>:PERFTRACE</c> directive
        /// </summary>
        /// <param name="od">The od.</param>
        /// <param name="fileName">Name of the file.</param>
        public virtual void PerfTrace(OutputDestination od, string fileName)
        {
        }

        /// <summary>
        /// Called when the parser has a complete batch to process.
        /// </summary>
        /// <param name="batch">The batch to process.</param>
        /// <param name="numberOfExecutions">The number of times to execute the batch (e.g. <c>GO 2</c> to execute the batch twice.</param>
        /// <remarks>
        /// If the current error mode (as set by <c>:ON ERROR</c>) is IGNORE, then any <see cref="SqlException" /> should be caught and
        /// sent to the STDERR channel, else it should be thrown and the client should handle it.
        /// </remarks>
        /// <inheritdoc />
        public virtual void ProcessBatch(SqlBatch batch, int numberOfExecutions)
        {
            if (this.arguments.ParseOnly)
            {
                return;    
            }

            var sql = batch.Sql;

            if (string.IsNullOrWhiteSpace(sql))
            {
                // Batch is empty. Don't round-trip the SQL server
                return;
            }

            // Get the query timeout
            var queryTimeout = 0;

            if (int.TryParse(this.variableResolver.ResolveVariable("SQLCMDSTATTIMEOUT"), out var t))
            {
                queryTimeout = t;
            }

            try
            {
                // For each execution (numeric argument to GO)
                for (var i = 0; i < numberOfExecutions; ++i)
                {
                    // Reset try count
                    var numTries = 0;

                    // Loop till command succeeds, non-retryable error or retry count exceeded
                    while (true)
                    {
                        ++numTries;

                        try
                        {
                            using (var command = this.connection.CreateCommand())
                            {
                                command.CommandType = CommandType.Text;
                                command.CommandText = sql;
                                command.CommandTimeout = queryTimeout;

                                if (this.resultsAs == OutputAs.None)
                                {
                                    command.ExecuteNonQuery();
                                }
                                else
                                {
                                    this.OutputWithResults(command);
                                }

                                // Success - exit while loop.
                                break;
                            }
                        }
                        catch (SqlException ex)
                        {
                            if (!IsRetryableError(ex) || numTries >= this.arguments.RetryCount)
                            {
                                // Can't retry this command
                                // Exit both the while loop and the go count as it will always fail.
                                throw;
                            }
                        }
                    }                
                }
            }
            catch (SqlException e)
            {
                this.SqlExceptions.Add(e);

                if (this.ErrorAction == ErrorAction.Exit)
                {
                    // Add batch data so that outer handler can format it.
                    e.Data.Add("Batch", batch);
                    throw;
                }

                this.WriteStderrMessage(e.Format(batch));

                // Indicate that errors have occurred during processing and continue
                this.arguments.ExitCode = 1;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>:QUIT</c> directive
        /// </summary>
        public virtual void Quit()
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>:RESET</c> directive.
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        ///   <c>:SERVERLIST</c> directive.
        /// </summary>
        /// <inheritdoc />
        public virtual void ServerList()
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// <c>:XML</c> directive.
        /// </summary>
        /// <param name="xs">The new XML status.</param>
        public virtual void Xml(XmlStatus xs)
        {
        }

        /// <summary>
        /// Determines whether the given exception represents a condition that can be retried.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <returns>
        ///   <c>true</c> if [is retryable error] [the specified ex]; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Examples of retryable errors:
        /// - Deadlock victim
        /// - Timeout (could be blocked)
        /// </remarks>
        private static bool IsRetryableError(SqlException ex)
        {
            return RetryableErrors.Any(e => ex.Number == e);
        }

        /// <summary>
        /// Tries to create a new output file in response to e.g. :OUT or :ERROR
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fileStream">The newly created file stream if the function succeeds.</param>
        /// <returns><c>true</c> if the file was created; else <c>false</c></returns>
        private static bool TryCreateOutputFile(string path, out Stream fileStream)
        {
            try
            {
                fileStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            }
            catch
            {
                fileStream = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Connects the specified connection string builder.
        /// </summary>
        /// <param name="connectionStringBuilder">The connection string builder.</param>
        private void DoConnect(SqlConnectionStringBuilder connectionStringBuilder)
        {
            // ReSharper disable StringLiteralTypo
            // Use a DbConnectionStringBuilder to test if values in the connection string are actually present. 
            // The SqlConnectionStringBuilder override always returns true on TryGetValue
            var dbc = new DbConnectionStringBuilder { ConnectionString = connectionStringBuilder.ConnectionString };

            if (dbc.TryGetValue("MultiSubnetFailover", out var o))
            {
                // Update from user supplied value
                this.variableResolver.SetSystemVariable("SQLCMDMULTISUBNETFAILOVER", o.ToString());
            }
            else
            {
                // Set from internal variables
                var multiSubnetFailover = this.variableResolver.ResolveVariable("SQLCMDMULTISUBNETFAILOVER");

                if (!string.IsNullOrWhiteSpace(multiSubnetFailover))
                {
                    connectionStringBuilder["MultiSubnetFailover"] = bool.Parse(multiSubnetFailover);
                }
            }

            if (dbc.TryGetValue("Packet Size", out var p))
            {
                // Update from user supplied value
                this.variableResolver.SetSystemVariable("SQLCMDPACKETSIZE", p.ToString());
            }
            else
            {
                // Set from internal variables
                connectionStringBuilder["Packet Size"] =
                    int.Parse(this.variableResolver.ResolveVariable("SQLCMDPACKETSIZE"));
            }

            try
            {
                this.connection = new SqlConnection(connectionStringBuilder.ConnectionString);
                this.connection.Open();
                this.connection.InfoMessage += this.OnSqlInfoMessageEvent;
            }
            catch (SqlException ex)
            {
                this.SqlExceptions.Add(ex);
                throw;
            }

            // Ensure the connection is properly closed when we do away with it.
            // Unit tests can fail with 'database in use' due to connections hanging around.
            SqlConnection.ClearPool(this.connection);

            this.variableResolver.SetSystemVariable("SQLCMDSERVER", this.connection.DataSource);
            this.variableResolver.SetSystemVariable(
                "SQLCMDUSER",
                connectionStringBuilder.IntegratedSecurity ? WindowsIdentity.GetCurrent().Name : connectionStringBuilder.UserID);
            this.variableResolver.SetSystemVariable(
                "SQLCMDPASSWORD",
                connectionStringBuilder.IntegratedSecurity ? string.Empty : connectionStringBuilder.Password);
            this.variableResolver.SetSystemVariable("SQLCMDDBNAME", this.connection.Database);

            this.Connected?.Invoke(this, new ConnectEventArgs(this.connection, this.stdoutDestination));

            // ReSharper restore StringLiteralTypo
        }

        /// <summary>
        /// Called when [SQL information message event].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SqlInfoMessageEventArgs"/> instance containing the event data.</param>
        private void OnSqlInfoMessageEvent(object sender, SqlInfoMessageEventArgs e)
        {
            this.WriteStdoutMessage(e.Message);
        }

        // ReSharper disable once CommentTypo

        /// <summary>
        /// Processes rows/tables/datasets.
        /// </summary>
        /// <param name="sqlCommand">The SQL command.</param>
        private void OutputWithResults(SqlCommand sqlCommand)
        {
            var scalarResultReturned = false;

            using (var sqlDataReader = sqlCommand.ExecuteReader())
            {
                var dataSet = new DataSet();

                do
                {
                    var schemaTable = sqlDataReader.GetSchemaTable();

                    if (schemaTable == null)
                    {
                        // There are no statements that create a result set in the batch
                        continue;
                    }

                    var dataTable = new DataTable();

                    foreach (var row in schemaTable.Rows.Cast<DataRow>())
                    {
                        var column = new DataColumn((string)row["ColumnName"], (Type)row["DataType"]);
                        dataTable.Columns.Add(column);
                    }

                    if (this.arguments.OutputAs == OutputAs.Scalar)
                    {
                        if (!scalarResultReturned && sqlDataReader.Read() && sqlDataReader.FieldCount > 0)
                        {
                            this.Result?.Invoke(this, new OutputResultEventArgs(sqlDataReader.GetValue(0)));
                            scalarResultReturned = true;
                        }
                    }
                    else
                    {
                        while (sqlDataReader.Read())
                        {
                            var newRow = dataTable.NewRow();
                            newRow.BeginEdit();

                            for (var i = 0; i < sqlDataReader.FieldCount; i++)
                            {
                                if (!sqlDataReader.IsDBNull(i))
                                {
                                    switch (sqlDataReader.GetDataTypeName(i).ToLowerInvariant())
                                    {
                                        case "char":
                                        case "varchar":
                                        case "text":
                                        case "xml":
                                        case "nchar":
                                        case "nvarchar":
                                        // ReSharper disable once StringLiteralTypo
                                        case "ntext":

                                            var text = sqlDataReader[i] as string;

                                            if (text != null && text.Length > this.arguments.MaxCharLength)
                                            {
                                                text = text.Substring(0, this.arguments.MaxCharLength);
                                            }

                                            newRow[i] = text;
                                            break;

                                        case "binary":
                                        case "varbinary":
                                        case "image":

                                            var array = new byte[this.arguments.MaxBinaryLength];
                                            sqlDataReader.GetBytes(i, 0L, array, 0, this.arguments.MaxBinaryLength);
                                            newRow[i] = array;
                                            break;

                                        default:

                                            newRow[i] = sqlDataReader[i];
                                            break;
                                    }
                                }
                            }

                            newRow.EndEdit();

                            if (this.resultsAs == OutputAs.DataRows)
                            {
                                this.Result?.Invoke(this, new OutputResultEventArgs(newRow));
                            }
                            else
                            {
                                dataTable.Rows.Add(newRow);
                            }
                        }
                    }

                    switch (this.resultsAs)
                    {
                        case OutputAs.DataTables:
                            this.Result?.Invoke(this, new OutputResultEventArgs(dataTable));
                            break;

                        case OutputAs.DataSet:
                            dataSet.Tables.Add(dataTable);
                            break;
                    }
                }
                while (sqlDataReader.NextResult());

                if (this.resultsAs == OutputAs.DataSet)
                {
                    this.Result?.Invoke(this, new OutputResultEventArgs(dataSet));
                }
            }
        }

        /// <summary>
        /// Writes the stderr message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void WriteStderrMessage(string message)
        {
            if (this.stderrDestination == OutputDestination.File)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                this.stderrFile.Write(bytes, 0, bytes.Length);

                // in case someone is tailing the file, flush to keep it up to date.
                this.stderrFile.Flush();
            }
            else
            {
                this.Message?.Invoke(this, new OutputMessageEventArgs(message, this.stderrDestination));
            }
        }

        /// <summary>
        /// Writes the stdout message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void WriteStdoutMessage(string message)
        {
            if (this.stdoutDestination == OutputDestination.File)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                this.stdoutFile.Write(bytes, 0, bytes.Length);

                // in case someone is tailing the file, flush to keep it up to date.
                this.stdoutFile.Flush();
            }
            else
            {
                this.Message?.Invoke(this, new OutputMessageEventArgs(message, this.stdoutDestination));
            }
        }

        /// <summary>
        /// Buffer object for output of <see cref="CommandExecuter.ExecuteShellCommand"/>
        /// </summary>
        private class ShellExecuteOutput
        {
            /// <summary>
            /// Gets or sets the output destination.
            /// </summary>
            /// <value>
            /// The output destination.
            /// </value>
            public OutputDestination OutputDestination { get; set; }

            /// <summary>
            /// Gets or sets the data.
            /// </summary>
            /// <value>
            /// The data.
            /// </value>
            public string Data { get; set; }
        }
    }
}