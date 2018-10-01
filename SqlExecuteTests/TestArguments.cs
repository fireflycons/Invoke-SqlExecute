// ReSharper disable InheritdocConsiderUsage
namespace SqlExecuteTests
{
    using System;
    using System.Collections;
    using System.Diagnostics;

    using Firefly.SqlCmdParser;
    using Firefly.SqlCmdParser.Client;

    /// <summary>
    /// Concrete argument class for these tests
    /// </summary>
    /// <seealso cref="Firefly.SqlCmdParser.Client.ISqlExecuteArguments" />
    internal class TestArguments : ISqlExecuteArguments
    {
        private string[] inputFile;

        /// <summary>
        /// Gets or sets a value indicating whether [abort on error].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [abort on error]; otherwise, <c>false</c>.
        /// </value>
        public bool AbortOnErrorSet { get; set; } = false;

        /// <summary>
        /// Gets or sets the connected event handler.
        /// </summary>
        /// <value>
        /// The connected.
        /// </value>
        public EventHandler<ConnectEventArgs> Connected { get; set; } = (sender, args) => { };
    
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string[] ConnectionString { get; set; }

        /// <summary>
        /// Gets the current directory resolver.
        /// </summary>
        /// <value>
        /// The current directory resolver. If <c>null</c> then <see cref="M:System.IO.Directory.GetCurrentDirectory" /> is used.
        /// </value>
        public ICurrentDirectoryResolver CurrentDirectoryResolver => null;

        /// <summary>
        /// Gets a value indicating whether to disable interactive commands, startup script, and environment variables.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [disable commands]; otherwise, <c>false</c>.
        /// </value>
        public bool DisableCommandsSet => false;

        /// <summary>
        /// Gets a value indicating whether [disable variables].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [disable variables]; otherwise, <c>false</c>.
        /// </value>
        public bool DisableVariablesSet => false;

        public bool RunParallel { get; }

        /// <summary>
        /// Gets or sets the exit code.
        /// </summary>
        /// <value>
        /// The exit code.
        /// </value>
        public int ExitCode { get; set; } = 0;

        /// <summary>
        /// Gets or sets the initial variables.
        /// </summary>
        /// <value>
        /// The initial variables.
        /// </value>
        public IDictionary InitialVariables { get; set; } = null;

        string[] ISqlExecuteArguments.InputFile => this.inputFile;

        /// <summary>
        /// Gets or sets the input file.
        /// </summary>
        /// <value>
        /// The input file.
        /// </value>
        public string InputFile { get; set; } = null;

        /// <summary>
        /// Gets the maximum length of binary data to return.
        /// </summary>
        public int MaxBinaryLength => 4096;

        /// <summary>
        /// Gets the maximum length of character data to return.
        /// </summary>
        /// <value>
        /// The maximum length of the character.
        /// </value>
        public int MaxCharLength => 4000;

        /// <summary>
        /// Gets the results as.
        /// </summary>
        /// <value>
        /// The results as.
        /// </value>
        public OutputAs OutputAs => OutputAs.DataRows;

        /// <summary>
        /// Gets the output file.
        /// </summary>
        /// <value>
        /// The output file.
        /// </value>
        public string OutputFile => null;

        /// <summary>
        /// Gets or sets the output message event handler.
        /// </summary>
        /// <value>
        /// The output message.
        /// </value>
        public EventHandler<OutputMessageEventArgs> OutputMessage { get; set; } = (sender, args) =>
            {
                switch (args.OutputDestination)
                {
                    case OutputDestination.StdOut:

                        Debug.WriteLine($"INFO : {args.Message}");
                        break;

                    case OutputDestination.StdError:

                        Debug.WriteLine($"ERROR: {args.Message}");
                        break;

                    case OutputDestination.File:

                        Debug.WriteLine($"FILE : {args.Message}");
                        break;
                }
            };

        /// <summary>
        /// Gets or sets the output result event handler.
        /// </summary>
        /// <value>
        /// The output result.
        /// </value>
        public EventHandler<OutputResultEventArgs> OutputResult { get; set; } = (sender, args) => { };

        /// <summary>
        /// Gets or sets the console/file result event handler.
        /// </summary>
        /// <value>
        /// The output result.
        /// </value>
        public EventHandler<OutputResultEventArgs> OutputTextResult { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [override script variables].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [override script variables]; otherwise, <c>false</c>.
        /// </value>
        public bool OverrideScriptVariablesSet { get; set; } = false;

        /// <summary>
        /// Gets a value indicating whether parse only (no execute) run should be performed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [parse only]; otherwise, <c>false</c>.
        /// </value>
        public bool ParseOnly => false;

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        /// <value>
        /// The query.
        /// </value>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets the query timeout.
        /// </summary>
        /// <value>
        /// The query timeout.
        /// </value>
        public int QueryTimeout { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of times to retry a retryable error (e.g. timed-out queries).
        /// </summary>
        /// <value>
        /// The retry count.
        /// </value>
        public int RetryCount { get; set; } = 0;
    }
}