namespace Firefly.SqlCmdParser.Client
{
    using System;
    using System.Collections;
    using System.IO;

    /// <summary>
    /// Defines the arguments for <see cref="SqlExecuteImpl" />
    /// </summary>
    public interface ISqlExecuteArguments
    {
        /// <summary>
        /// Gets a value indicating whether [abort on error].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [abort on error]; otherwise, <c>false</c>.
        /// </value>
        bool AbortOnErrorSet { get; }

        /// <summary>
        /// Gets the connected event handler.
        /// </summary>
        /// <value>
        /// The connected.
        /// </value>
        EventHandler<ConnectEventArgs> Connected { get; }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        string ConnectionString { get; }

        /// <summary>
        /// Gets the current directory resolver.
        /// </summary>
        /// <value>
        /// The current directory resolver. If <c>null</c> then <see cref="Directory.GetCurrentDirectory"/> is used.
        /// </value>
        ICurrentDirectoryResolver CurrentDirectoryResolver { get; }

        /// <summary>
        /// Gets a value indicating whether to disable interactive commands, startup script, and environment variables.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [disable commands]; otherwise, <c>false</c>.
        /// </value>
        bool DisableCommandsSet { get; }

        /// <summary>
        /// Gets a value indicating whether parse only (no execute) run should be performed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [parse only]; otherwise, <c>false</c>.
        /// </value>
        bool ParseOnly { get; }

        /// <summary>
        /// Gets a value indicating whether [disable variables].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [disable variables]; otherwise, <c>false</c>.
        /// </value>
        bool DisableVariablesSet { get; }

        /// <summary>
        /// Gets the initial variables.
        /// </summary>
        /// <value>
        /// The initial variables.
        /// </value>
        IDictionary InitialVariables { get; }

        /// <summary>
        /// Gets the input file.
        /// </summary>
        /// <value>
        /// The input file.
        /// </value>
        string InputFile { get; }

        /// <summary>
        /// Gets the maximum length of binary data to return.
        /// </summary>
        int MaxBinaryLength { get; }

        /// <summary>
        /// Gets the maximum length of character data to return.
        /// </summary>
        /// <value>
        /// The maximum length of the character.
        /// </value>
        int MaxCharLength { get; }

        /// <summary>
        /// Gets the results as.
        /// </summary>
        /// <value>
        /// The results as.
        /// </value>
        OutputAs OutputAs { get; }

        /// <summary>
        /// Gets the output file.
        /// </summary>
        /// <value>
        /// The output file.
        /// </value>
        string OutputFile { get; }

        /// <summary>
        /// Gets the output message event handler.
        /// </summary>
        /// <value>
        /// The output message.
        /// </value>
        EventHandler<OutputMessageEventArgs> OutputMessage { get; }

        /// <summary>
        /// Gets the output result event handler.
        /// </summary>
        /// <value>
        /// The output result.
        /// </value>
        EventHandler<OutputResultEventArgs> OutputResult { get; }

        /// <summary>
        /// Gets a value indicating whether [override script variables].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [override script variables]; otherwise, <c>false</c>.
        /// </value>
        bool OverrideScriptVariablesSet { get; }

        /// <summary>
        /// Gets the query.
        /// </summary>
        /// <value>
        /// The query.
        /// </value>
        string Query { get; }

        /// <summary>
        /// Gets the query timeout.
        /// </summary>
        /// <value>
        /// The query timeout.
        /// </value>
        int QueryTimeout { get; }

        /// <summary>
        /// Gets the number of times to retry a retiable error (e.g. timed-out queries).
        /// </summary>
        /// <value>
        /// The retry count.
        /// </value>
        int RetryCount { get; }

        /// <summary>
        /// Gets or sets the exit code.
        /// </summary>
        /// <value>
        /// The exit code.
        /// </value>
        int ExitCode { get; set; }
    }
}