namespace Firefly.SqlCmdParser.Client
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Diagnostics;

    /// <inheritdoc />
    /// <summary>
    /// This class does the actual work of running the supplied SQL
    /// </summary>
    public class SqlExecuteImpl : IDisposable
    {
        /// <summary>
        /// The arguments
        /// </summary>
        private readonly ISqlExecuteArguments arguments;

        /// <summary>
        /// The executer
        /// </summary>
        private readonly CommandExecuter executer;

        /// <summary>
        /// The variable resolver
        /// </summary>
        private readonly VariableResolver variableResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecuteImpl"/> class.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        public SqlExecuteImpl(ISqlExecuteArguments arguments)
        {
            this.arguments = arguments;
            this.variableResolver = new VariableResolver(
                this.arguments.InitialVariables,
                this.arguments.OverrideScriptVariablesSet,
                this.arguments.DisableCommandsSet);

            if (this.arguments.ParseOnly)
            {
                this.arguments.OutputMessage?.Invoke(this, new OutputMessageEventArgs("DRY RUN mode - No SQL will be executed.", OutputDestination.StdOut));    
            }

            this.variableResolver.SetSystemVariable("SQLCMDSTATTIMEOUT", this.arguments.QueryTimeout.ToString());

            this.executer =
                new CommandExecuter(this.arguments, this.variableResolver)
                    {
                        ErrorAction =
                            arguments.AbortOnErrorSet
                                ? ErrorAction.Exit
                                : ErrorAction.Ignore
                    };

            if (!string.IsNullOrEmpty(this.arguments.OutputFile))
            {
                this.executer.Out(OutputDestination.File, this.arguments.OutputFile);
            }

            if (this.arguments.OutputMessage != null)
            {
                this.executer.Message += this.arguments.OutputMessage;
            }

            if (this.arguments.OutputResult != null)
            {
                this.executer.Result += this.arguments.OutputResult;
            }

            if (this.arguments.Connected != null)
            {
                this.executer.Connected += this.arguments.Connected;
            }
        }

        /// <summary>
        /// Gets the list of SQL exceptions thrown during the batch execution.
        /// </summary>
        /// <value>
        /// The SQL exceptions.
        /// </value>
        public IList<SqlException> SqlExceptions => this.executer == null ? new List<SqlException>() : this.executer.SqlExceptions;

        /// <summary>
        /// Gets the error count.
        /// </summary>
        /// <value>
        /// The error count.
        /// </value>
        public int ErrorCount { get; private set; }

        /// <summary>
        /// Gets the batch count.
        /// </summary>
        /// <value>
        /// The batch count.
        /// </value>
        public int BatchCount { get; private set; }


        /// <inheritdoc />
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            this.executer?.Dispose();
        }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        public void Execute()
        {
            this.executer.ConnectWithConnectionString(this.arguments.ConnectionString);

            var parser = new Parser(
                this.arguments.DisableCommandsSet,
                false,
                this.executer,
                this.variableResolver,
                this.arguments.CurrentDirectoryResolver);

            parser.InputSourceChanged += this.ParserOnInputSourceChanged;

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                parser.Parse(this.GetInitialBatchSource());
            }
            finally
            {
                sw.Stop();
                this.BatchCount = parser.BatchCount;
                this.ErrorCount = this.executer.ErrorCount;
                var batch = parser.BatchCount == 1 ? "batch" : "batches";
                var error = this.executer.ErrorCount == 1 ? "error" : "errors";
                this.arguments.OutputMessage?.Invoke(this, new OutputMessageEventArgs($"{parser.BatchCount} {batch} processed in {sw.Elapsed.Minutes} min, {sw.Elapsed.Seconds}.{sw.Elapsed.Milliseconds:D3} sec.", OutputDestination.StdOut));
                this.arguments.OutputMessage?.Invoke(this, new OutputMessageEventArgs($"{this.executer.ErrorCount} SQL {error} in execution.", OutputDestination.StdOut));
            }
        }

        /// <summary>
        /// Gets the initial batch source.
        /// </summary>
        /// <returns>An <see cref="IBatchSource"/> for the initial SQL input.</returns>
        /// <exception cref="InvalidOperationException">
        /// Either an input file or a query string is required
        /// or
        /// Cannot specify both an input file and a query string
        /// </exception>
        private IBatchSource GetInitialBatchSource()
        {
            if (string.IsNullOrEmpty(this.arguments.Query) && string.IsNullOrEmpty(this.arguments.InputFile))
            {
                throw new InvalidOperationException("Either an input file or a query string is required");
            }

            if (!string.IsNullOrEmpty(this.arguments.Query) && !string.IsNullOrEmpty(this.arguments.InputFile))
            {
                throw new InvalidOperationException("Cannot specify both an input file and a query string");
            }

            if (!string.IsNullOrEmpty(this.arguments.Query))
            {
                return new BatchSourceString(this.arguments.Query);
            }

            return new BatchSourceFile(this.arguments.InputFile);
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
        /// Handles <see cref="Parser.InputSourceChanged"/> event
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="inputSourceChangedEventArgs">The <see cref="InputSourceChangedEventArgs"/> instance containing the event data.</param>
        private void ParserOnInputSourceChanged(object sender, InputSourceChangedEventArgs inputSourceChangedEventArgs)
        {
            this.arguments.OutputMessage?.Invoke(
                this,
                new OutputMessageEventArgs(
                    $"Input Source: '{inputSourceChangedEventArgs.Source.Filename}', Encoding: {inputSourceChangedEventArgs.Source.Encoding}",
                    OutputDestination.StdOut));
        }
    }
}