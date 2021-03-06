﻿namespace Firefly.SqlCmdParser
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;

    /// <summary>
    /// Interface that defines methods for handling SQLCMD commands.
    /// Implementation methods should throw if the parse-process mechanism should stop.
    /// </summary>
    public interface ICommandExecuter : IDisposable
    {
        /// <summary>
        /// Occurs when a database connection is made
        /// </summary>
        event EventHandler<ConnectEventArgs> Connected;

        /// <summary>
        /// Occurs when a message is ready.
        /// </summary>
        event EventHandler<OutputMessageEventArgs> Message;

        /// <summary>
        /// Occurs when a result or result set is ready.
        /// </summary>
        event EventHandler<OutputResultEventArgs> Result;

        /// <summary>
        /// Gets the custom exit code set by :EXIT(query).
        /// </summary>
        /// <value>
        /// The custom exit code. If <c>null</c> then :EXIT was not encountered.
        /// </value>
        int? CustomExitCode { get; }

        /// <summary>
        /// Gets the number of <see cref="SqlException"/> errors recorded by <see cref="ProcessBatch"/>.
        /// Retriable errors that retried and then successfully executed are not counted.
        /// </summary>
        /// <value>
        /// The error count.
        /// </value>
        int ErrorCount { get; }

        /// <summary>
        /// Gets the execution node number.
        /// </summary>
        /// <value>
        /// The node number, needed for raising message events.
        /// </value>
        int NodeNumber { get; }

        /// <summary>
        /// Gets the list of SQL exceptions thrown during the batch execution.
        /// </summary>
        /// <value>
        /// The SQL exceptions.
        /// </value>
        IList<SqlException> SqlExceptions { get; }

        /// <summary>
        /// Gets the current destination for STDERR messages.
        /// </summary>
        /// <value>
        /// The destination.
        /// </value>
        OutputDestination StderrDestination { get; }

        /// <summary>
        /// Gets the current destination for STDOUT messages.
        /// </summary>
        /// <value>
        /// The destination.
        /// </value>
        OutputDestination StdoutDestination { get; }

        /// <summary>
        /// <c>:CONNECT</c> directive.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="server">The server.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        void Connect(int timeout, string server, string user, string password);

        /// <summary>
        /// Connects the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        void ConnectWithConnectionString(string connectionString);

        /// <summary>
        ///   <c>:ED</c> directive
        /// </summary>
        /// <param name="batch">The current batch.</param>
        /// <returns>The edited batch as a new <see cref="IBatchSource"/>; or <c>null</c> if no changes were made.</returns>
        IBatchSource Ed(string batch);

        /// <summary>
        /// <c>:ERROR</c> directive
        /// </summary>
        /// <param name="od">The od.</param>
        /// <param name="fileName">Name of the file.</param>
        void Error(OutputDestination od, string fileName);

        /// <summary>
        /// <c>!!</c> directive.
        /// </summary>
        /// <param name="command">The command.</param>
        void ExecuteShellCommand(string command);

        /// <summary>
        /// <c>:EXIT</c> directive
        /// </summary>
        /// <param name="batch">The current batch.</param>
        /// <param name="exitBatch">The exit batch.</param>
        void Exit(SqlBatch batch, string exitBatch);

        /// <summary>
        /// <c>:HELP</c> directive
        /// </summary>
        void Help();

        /// <summary>
        ///   <c>:R</c> directive.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>New <see cref="IBatchSource"/> to read from.</returns>
        IBatchSource IncludeFileName(string fileName);

        /// <summary>
        /// <c>:LIST</c> directive.
        /// </summary>
        /// <param name="batch">The batch.</param>
        /// <remarks>
        /// Implementations may do additional formatting.
        /// </remarks>
        void List(string batch);

        /// <summary>
        /// <c>:LISTVAR</c> directive.
        /// </summary>
        /// <param name="varList">The variable list.</param>
        /// <remarks>
        /// Implementation may decide to redact passwords etc.
        /// </remarks>
        void ListVar(IDictionary<string, string> varList);

        /// <summary>
        /// <c>:ON ERROR</c> directive.
        /// </summary>
        /// <param name="ea">The error action.</param>
        void OnError(ErrorAction ea);

        /// <summary>
        /// <c>:OUT</c> directive.
        /// </summary>
        /// <param name="outputDestination">The od.</param>
        /// <param name="fileName">Name of the file.</param>
        void Out(OutputDestination outputDestination, string fileName);

        /// <summary>
        ///   <c>:OUT</c> directive.
        /// </summary>
        /// <param name="outputDestination">The od.</param>
        /// <param name="outputFileProperties">The output file properties.</param>
        void Out(OutputDestination outputDestination, IOutputFileProperties outputFileProperties);

        /// <summary>
        /// <c>:PERFTRACE</c> directive
        /// </summary>
        /// <param name="od">The od.</param>
        /// <param name="fileName">Name of the file.</param>
        void PerfTrace(OutputDestination od, string fileName);

        /// <summary>
        /// Called when the parser has a complete batch to process.
        /// Implementations should check for SQL being empty or whitespace and not send to the server if this is so (performance).
        /// </summary>
        /// <param name="batch">The batch to process.</param>
        /// <param name="numberOfExecutions">The number of times to execute the batch (e.g. <c>GO 2</c> to execute the batch twice.</param>
        /// <remarks>
        /// If the current error mode (as set by <c>:ON ERROR</c>) is IGNORE, then any <see cref="SqlException"/> should be caught and
        /// sent to the STDERR channel, else it should be thrown and the client should handle it.
        /// </remarks>
        void ProcessBatch(SqlBatch batch, int numberOfExecutions);

        /// <summary>
        /// <c>:QUIT</c> directive
        /// </summary>
        void Quit();

        /// <summary>
        /// <c>:RESET</c> directive.
        /// </summary>
        void Reset();

        /// <summary>
        /// <c>:SERVERLIST</c> directive.
        /// </summary>
        void ServerList();

        /// <summary>
        /// <c>:XML</c> directive.
        /// </summary>
        /// <param name="xs">The xs.</param>
        void Xml(XmlStatus xs);

        /// <summary>
        /// Called when [input source changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="InputSourceChangedEventArgs"/> instance containing the event data.</param>
        void OnInputSourceChanged(object sender, InputSourceChangedEventArgs args);

        /// <summary>
        /// Writes a message to current <c>stderr</c> destination
        /// /// </summary>
        /// <param name="message">The message.</param>
        void WriteStderrMessage(string message);

        /// <summary>
        /// Writes a message to current <c>stdout</c> destination
        /// /// </summary>
        /// <param name="message">The message.</param>
        void WriteStdoutMessage(string message);
    }
}