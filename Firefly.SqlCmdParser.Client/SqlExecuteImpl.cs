namespace Firefly.SqlCmdParser.Client
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    /// <inheritdoc />
    /// <summary>
    /// This class does the actual work of running the supplied SQL
    /// </summary>
    public class SqlExecuteImpl : IDisposable
    {
        /// <summary>
        /// The args
        /// </summary>
        private readonly ISqlExecuteArguments arguments;

        /// <summary>
        /// The run list
        /// </summary>
        private readonly List<RunConfiguration> runList;

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
                    new OutputMessageEventArgs(0, "DRY RUN mode - No SQL will be executed.", OutputDestination.StdOut));
            }

            this.runList = new List<RunConfiguration>();

            for (var i = 0; i < Math.Max(arguments.ConnectionString.Length, arguments.InputFile?.Length ?? 0); ++i)
            {
                var r = CreateVariableResolver(arguments);
                var e = CreateCommandExecuter(i + 1, arguments, r);

                this.runList.Add(
                    new RunConfiguration
                        {
                            NodeNumber = i + 1,
                            CommandExecuter = e,
                            ConnectionString =
                                arguments.ConnectionString.Length == 1
                                    ? arguments.ConnectionString[0]
                                    : arguments.ConnectionString[i],
                            InitialBatchSource = this.GetInitialBatchSource(i),
                            VariableResolver = r
                        });
            }
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
        public IList<SqlException> SqlExceptions
        {
            get
            {
                var e = new List<SqlException>();

                foreach (var r in this.runList)
                {
                    if (r.CommandExecuter != null)
                    {
                        e.AddRange(r.CommandExecuter.SqlExceptions);
                    }
                }

                return e;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var r in this.runList)
            {
                r.CommandExecuter?.Dispose();
            }
        }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        public void Execute()
        {
            if (this.arguments.RunParallel)
            {
                var tasks = this.runList.Select(
                    r =>
                        {
                            return Task.Factory.StartNew(
                                () =>
                                    {
                                        r.CommandExecuter.ConnectWithConnectionString(r.ConnectionString);
                                        this.InvokeParser(r.NodeNumber, r);
                                    });
                        });

                Task.WaitAll(tasks.ToArray());
            }
            else
            {
                foreach (var r in this.runList)
                {
                    r.CommandExecuter.ConnectWithConnectionString(r.ConnectionString);

                    // Invoke with invocationNumber = 0, meaning not parallel
                    this.InvokeParser(0, r);
                }
            }
        }

        /// <summary>
        /// Gets the highest error level set via :EXIT(query) or :SETVAR SQLCMDERRORLEVEL across all run configurations.
        /// </summary>
        /// <returns>The error level.</returns>
        public int GetErrorLevel()
        {
            return this.runList.Max(
                r => r.CommandExecuter.CustomExitCode ?? (int.TryParse(
                                                              r.VariableResolver.ResolveVariable("SQLCMDERRORLEVEL"),
                                                              out var errorLevel)
                                                              ? errorLevel
                                                              : 0));
        }

        /// <summary>
        /// Creates a command executer.
        /// </summary>
        /// <param name="nodeNumber">The node number.</param>
        /// <param name="args">The command invocation arguments.</param>
        /// <param name="resolver">The resolver.</param>
        /// <returns>
        /// A new <see cref="ICommandExecuter" />
        /// </returns>
        private static ICommandExecuter CreateCommandExecuter(int nodeNumber, ISqlExecuteArguments args, IVariableResolver resolver)
        {
            var exec = new CommandExecuter(nodeNumber, args, resolver)
                           {
                               ErrorAction =
                                   args.AbortOnErrorSet
                                       ? ErrorAction.Exit
                                       : ErrorAction.Ignore
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
        /// <param name="nodeNumber">The execution node number - zero if running sequentially</param>
        /// <param name="runConfiguration">The run configuration.</param>
        private void InvokeParser(int nodeNumber, RunConfiguration runConfiguration)
        {
            var parser = new Parser(
                nodeNumber,
                runConfiguration,
                this.arguments.DisableCommandsSet,
                this.arguments.DisableVariablesSet,
                this.arguments.CurrentDirectoryResolver);

            parser.InputSourceChanged += this.ParserOnInputSourceChanged;

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                parser.Parse();
            }
            finally
            {
                sw.Stop();

                this.BatchCount += parser.BatchCount;
                this.ErrorCount += runConfiguration.CommandExecuter.ErrorCount;
                var batch = parser.BatchCount == 1 ? "batch" : "batches";
                var error = runConfiguration.CommandExecuter.ErrorCount == 1 ? "error" : "errors";
                this.arguments.OutputMessage?.Invoke(
                    this,
                    new OutputMessageEventArgs(
                        nodeNumber,
                        $"{parser.BatchCount} {batch} processed in {sw.Elapsed.Minutes} min, {sw.Elapsed.Seconds}.{sw.Elapsed.Milliseconds:D3} sec.",
                        OutputDestination.StdOut));
                this.arguments.OutputMessage?.Invoke(
                    this,
                    new OutputMessageEventArgs(
                        nodeNumber,
                        $"{runConfiguration.CommandExecuter.ErrorCount} SQL {error} in execution.",
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
                    inputSourceChangedEventArgs.NodeNumber,
                    $"Input Source: '{inputSourceChangedEventArgs.Source.Filename}', Encoding: {inputSourceChangedEventArgs.Source.Encoding}",
                    OutputDestination.StdOut));
        }
    }
}