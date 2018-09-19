namespace Firefly.SqlCmdParser
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Represents a single line of text read from the input stream along with a reference to where it was read from.
    /// </summary>
    [DebuggerDisplay("Line {LineNumber}: {Source.Filename}")]
    internal class SqlBatchItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlBatchItem"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="source">The source.</param>
        public SqlBatchItem(string text, IBatchSource source)
        {
            // Remove any line terminators
            this.Text = text.TrimEnd(Environment.NewLine.ToCharArray());
            this.Source = source;
            this.LineNumber = source.CurrentLineNumber;
        }

        /// <summary>
        /// Gets the line number within the batch source that this text is on.
        /// </summary>
        /// <value>
        /// The line number.
        /// </value>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the batch source (mainly as a reference to input file name).
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public IBatchSource Source { get; }

        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; }
    }
}