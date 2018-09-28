namespace Firefly.SqlCmdParser
{
    using System;

    /// <summary>
    /// Base class for event arguments that carry an execution node number
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ParallelNodeEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelNodeEventArgs" /> class.
        /// </summary>
        /// <param name="nodeNumber">The node number.</param>
        /// <param name="outputDestination">The output destination.</param>
        protected ParallelNodeEventArgs(int nodeNumber, OutputDestination outputDestination)
        {
            this.NodeNumber = nodeNumber;
        }

        /// <summary>
        /// Gets the invocation number (greater than zero if parallel execution).
        /// </summary>
        /// <value>
        /// The invocation number.
        /// </value>
        public int NodeNumber { get; }

        /// <summary>
        /// Gets the output destination.
        /// </summary>
        /// <value>
        /// The output destination.
        /// </value>
        public OutputDestination OutputDestination { get; }
    }
}