namespace Firefly.SqlCmdParser.Client
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Threading;

    /// <inheritdoc />
    /// <summary>
    /// This class does the actual work of running the supplied SQL
    /// </summary>
    public class SqlExecuteImpl : IDisposable
    {
        /// <summary>
        /// Id of the thread we wre invoked from.
        /// </summary>
        private static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;

        /// <summary>
        /// The args
        /// </summary>
        private readonly ISqlExecuteArguments arguments;

        /// <summary>
        /// The executer
        /// </summary>
        private readonly ICommandExecuter executer;

        /// <summary>
        /// The variable resolver
        /// </summary>
        private readonly IVariableResolver variableResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecuteImpl"/> class.
        /// </summary>
        /// <param name="arguments">The args.</param>
        public SqlExecuteImpl(ISqlExecuteArguments arguments)
        {
            this.arguments = arguments;

            if (this.arguments.ParseOnly)
            {
                this.arguments.OutputMessage?.Invoke(
                    this,
                    new OutputMessageEventArgs("DRY RUN mode - No SQL will be executed.", OutputDestination.StdOut));
            }

            // TODO: Create an ordered run configuration list

            var resolver = CreateVariableResolver(arguments);
            var exec = CreateCommandExecuter(arguments, resolver);

            this.variableResolver = resolver;
            this.executer = exec;
        }

        /// <summary>
        /// Gets the batch count.
        /// </summary>
        /// <value>
        /// The batch count.
        /// </value>
        public int BatchCount { get; private set; }

        /// <summary>
        /// Gets the error count.
        /// </summary>
        /// <value>
        /// The error count.
        /// </value>
        public int ErrorCount { get; private set; }

        /// <summary>
        /// Gets the list of SQL exceptions thrown during the batch execution.
        /// </summary>
        /// <value>
        /// The SQL exceptions.
        /// </value>
        public IList<SqlException> SqlExceptions =>
            this.executer == null ? new List<SqlException>() : this.executer.SqlExceptions;

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

            if (this.arguments.RunParallel)
            {
                throw new NotImplementedException();
            }
            else
            {
                // TODO: Step through connections/input files
                this.InvokeParser(0);
            }
        }

        /// <summary>
        /// Gets any error level set via :EXIT(query) or :SETVAR SQLCMDERRORLEVEL.
        /// </summary>
        /// <returns>The error level.</returns>
        public int GetErrorLevel()
        {
            // ReSharper disable once StyleCop.SA1117
            return this.executer.CustomExitCode ?? (int.TryParse(
                                                        this.variableResolver.ResolveVariable("SQLCMDERRORLEVEL"),
                                                        out var errorLevel)
                                                        ? errorLevel
                                                        : 0);
        }

        /// <summary>
        /// Creates a command executer.
        /// </summary>
        /// <param name="args">The command invocation arguments.</param>
        /// <param name="resolver">The resolver.</param>
        /// <returns>A new <see cref="ICommandExecuter"/></returns>
        private static ICommandExecuter CreateCommandExecuter(ISqlExecuteArguments args, IVariableResolver resolver)
        {
            var exec = new CommandExecuter(args, resolver)
                           {
                               ErrorAction = args.AbortOnErrorSet ? ErrorAction.Exit : ErrorAction.Ignore
                           };

            if (!string.IsNullOrEmpty(args.OutputFile))
            {
                exec.Out(OutputDestination.File, args.OutputFile);
            }

            if (args.OutputMessage != null)
            {
                exec.Message += args.OutputMessage;
            }

            if (args.OutputResult != null)
            {
                exec.Result += args.OutputResult;
            }

            if (args.Connected != null)
            {
                exec.Connected += args.Connected;
            }

            return exec;
        }

        /// <summary>
        /// Creates a variable resolver.
        /// </summary>
        /// <param name="args">The command invocation arguments.</param>
        /// <returns>A new <see cref="IVariableResolver"/></returns>
        private static IVariableResolver CreateVariableResolver(ISqlExecuteArguments args)
        {
            var resolver = new VariableResolver(
                args.InitialVariables,
                args.OverrideScriptVariablesSet,
                args.DisableCommandsSet);

            resolver.SetSystemVariable("SQLCMDSTATTIMEOUT", args.QueryTimeout.ToString());
            return resolver;
        }

        /// <summary>
        /// Gets the initial batch source.
        /// </summary>
        /// <param name="index">Index into the input file array</param>
        /// <returns>An <see cref="IBatchSource"/> for the initial SQL input.</returns>
        /// <exception cref="InvalidOperationException">
        /// Either an input file or a query string is required
        /// or
        /// Cannot specify both an input file and a query string
        /// </exception>
        private IBatchSource GetInitialBatchSource(int index)
        {
            if (!string.IsNullOrEmpty(this.arguments.Query))
            {
                return new BatchSourceString(this.arguments.Query);
            }

            return new BatchSourceFile(this.arguments.InputFile[index]);
        }

        /// <summary>
        /// Invokes the parser.
        /// </summary>
        /// <param name="thisInvocationNumber">The invocation number - zero if running sequentially</param>
        private void InvokeParser(int thisInvocationNumber)
        {
            var parser = new Parser(
                thisInvocationNumber,
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
                parser.Parse(
                    this.GetInitialBatchSource(
                        this.arguments.InputFile == null || this.arguments.InputFile.Length <= 1
                            ? 0
                            : thisInvocationNumber));
            }
            finally
            {
                sw.Stop();

                this.BatchCount = parser.BatchCount;
                this.ErrorCount = this.executer.ErrorCount;
                var batch = parser.BatchCount == 1 ? "batch" : "batches";
                var error = this.executer.ErrorCount == 1 ? "error" : "errors";
                this.arguments.OutputMessage?.Invoke(
                    this,
                    new OutputMessageEventArgs(
                        $"{parser.BatchCount} {batch} processed in {sw.Elapsed.Minutes} min, {sw.Elapsed.Seconds}.{sw.Elapsed.Milliseconds:D3} sec.",
                        OutputDestination.StdOut));
                this.arguments.OutputMessage?.Invoke(
                    this,
                    new OutputMessageEventArgs(
                        $"{this.executer.ErrorCount} SQL {error} in execution.",
                        OutputDestination.StdOut));
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

        /// <summary>
        /// Run configuration
        /// </summary>
        private class RunConfiguration
        {
            /// <summary>
            /// Gets or sets the command executer.
            /// </summary>
            /// <value>
            /// The command executer.
            /// </value>
            public ICommandExecuter CommandExecuter { get; set; }

            /// <summary>
            /// Gets or sets the connection string.
            /// </summary>
            /// <value>
            /// The connection string.
            /// </value>
            public string ConnectionString { get; set; }

            /// <summary>
            /// Gets or sets the initial batch source.
            /// </summary>
            /// <value>
            /// The initial batch source.
            /// </value>
            public IBatchSource InitialBatchSource { get; set; }

            /// <summary>
            /// Gets or sets the variable resolver.
            /// </summary>
            /// <value>
            /// The variable resolver.
            /// </value>
            public IVariableResolver VariableResolver { get; set; }
        }
    }
}