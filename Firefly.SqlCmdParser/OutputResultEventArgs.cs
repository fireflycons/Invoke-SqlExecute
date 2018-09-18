namespace Firefly.SqlCmdParser
{
    using System;

    /// <inheritdoc />
    /// <summary>
    /// Arguments for event raised when SQL execution produces a result
    /// </summary>
    /// <seealso cref="T:System.EventArgs" />
    public class OutputResultEventArgs : EventArgs
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.OutputResultEventArgs" /> class.
        /// </summary>
        /// <param name="result">The result.</param>
        public OutputResultEventArgs(dynamic result)
        {
            this.Result = result;
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>
        /// The result.
        /// </value>
        public dynamic Result { get; }
    }
}
