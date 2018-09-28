namespace Firefly.SqlCmdParser
{
    /// <summary>
    /// Arguments for event raised when the input source changes.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class InputSourceChangedEventArgs : ParallelNodeEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.InputSourceChangedEventArgs" /> class.
        /// </summary>
        /// <param name="nodeNumber">The execution node number.</param>
        /// <param name="newSource">The new source.</param>
        /// <param name="outputDestination">The output destination.</param>
        /// <inheritdoc />
        internal InputSourceChangedEventArgs(int nodeNumber, IBatchSource newSource, OutputDestination outputDestination)
            : base(nodeNumber, outputDestination)
        {
            this.Source = newSource;
        }

        /// <summary>
        /// Gets the new source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public IBatchSource Source { get; }
    }
}