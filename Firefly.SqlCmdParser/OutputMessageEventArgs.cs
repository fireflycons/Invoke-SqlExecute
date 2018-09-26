namespace Firefly.SqlCmdParser
{
    using System;

    /// <inheritdoc />
    /// <summary>
    /// Event arguments for messages that should be output
    /// </summary>
    /// <seealso cref="T:System.EventArgs" />
    public class OutputMessageEventArgs : ParallelNodeEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.OutputMessageEventArgs" /> class.
        /// </summary>
        /// <param name="nodeNumber"></param>
        /// <param name="message">The message.</param>
        /// <param name="outputDestination">The output destination.</param>
        /// <inheritdoc />
        public OutputMessageEventArgs(int nodeNumber, string message, OutputDestination outputDestination)
        : base(nodeNumber)
        {
            this.Message = message;
            this.OutputDestination = outputDestination;
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; }

        /// <summary>
        /// Gets the output destination.
        /// </summary>
        /// <value>
        /// The output destination.
        /// </value>
        public OutputDestination OutputDestination { get; }
    }
}