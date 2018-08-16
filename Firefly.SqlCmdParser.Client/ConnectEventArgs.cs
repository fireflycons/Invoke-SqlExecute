namespace Firefly.SqlCmdParser.Client
{
    using System;
    using System.Data.SqlClient;

    /// <inheritdoc />
    /// <summary>
    /// Arguments for Connect event in <see cref="T:Firefly.SqlCmdParser.Client.CommandExecuter" />
    /// </summary>
    /// <seealso cref="T:System.EventArgs" />
    public class ConnectEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.Client.ConnectEventArgs" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="outputDestination">The output destination.</param>
        /// <inheritdoc />
        public ConnectEventArgs(SqlConnection connection, OutputDestination outputDestination)
        {
            this.Connection = connection;
            this.OutputDestination = outputDestination;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public SqlConnection Connection { get; }

        /// <summary>
        /// Gets the current output destination for stdout.
        /// </summary>
        /// <value>
        /// The output destination.
        /// </value>
        public OutputDestination OutputDestination { get; }
    }
}