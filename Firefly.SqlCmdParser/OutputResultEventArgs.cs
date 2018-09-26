namespace Firefly.SqlCmdParser
{
    using System;
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Arguments for event raised when SQL execution produces a result that should be emitted to the
    /// </summary>
    /// <seealso cref="T:System.EventArgs" />
    public class OutputResultEventArgs : ParallelNodeEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.OutputResultEventArgs" /> class.
        /// </summary>
        /// <param name="nodeNumber">The execution node number.</param>
        /// <param name="result">The result set, e.g. <c>DataTable</c> or <c>DataReader</c></param>
        /// <param name="outputDestination">The output destination.</param>
        /// <param name="outputStream">The output stream to write to if the destination is file.</param>
        /// <inheritdoc />
        public OutputResultEventArgs(int nodeNumber, dynamic result, OutputDestination outputDestination, Stream outputStream)
        : base(nodeNumber)
        {
            this.Result = result;
            this.OutputDestination = outputDestination;
            this.OutputStream = outputStream;
        }

        /// <summary>
        /// Gets the output destination.
        /// </summary>
        /// <value>
        /// The output destination.
        /// </value>
        public OutputDestination OutputDestination { get; }

        /// <summary>
        /// Gets the output stream.
        /// </summary>
        /// <value>
        /// The output stream.
        /// </value>
        public Stream OutputStream { get; }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>
        /// The result.
        /// </value>
        public dynamic Result { get; }
    }
}