namespace Firefly.SqlCmdParser
{
    using System.Data.SqlClient;

    /// <inheritdoc />
    /// <summary>
    /// Arguments for Connect event in <see cref="T:Firefly.SqlCmdParser.Client.CommandExecuter" />
    /// </summary>
    /// <seealso cref="T:System.EventArgs" />
    public class ConnectEventArgs : ParallelNodeEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.ConnectEventArgs" /> class.
        /// </summary>
        /// <param name="nodeNumber">The execution node number.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="outputDestination">The output destination.</param>
        /// <inheritdoc />
        public ConnectEventArgs(int nodeNumber, SqlConnection connection, OutputDestination outputDestination)
        : base(nodeNumber)
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
        /// Gets the current output destination for <c>stdout</c>.
        /// </summary>
        /// <value>
        /// The output destination.
        /// </value>
        public OutputDestination OutputDestination { get; }
    }
}