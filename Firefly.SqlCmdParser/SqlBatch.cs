namespace Firefly.SqlCmdParser
{
    using System.Text;

    /// <summary>
    /// Object that compiles a batch of SQL
    /// </summary>
    public class SqlBatch
    {
        /// <summary>
        /// The batch
        /// </summary>
        private readonly StringBuilder batch = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlBatch"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        internal SqlBatch(IBatchSource source)
        {
            this.Source = source.Filename;
            this.BatchBeginLineNumber = source.CurrentLineNumber + 1;
        }

        /// <summary>
        /// Gets the line number within the source that the batch begins at.
        /// </summary>
        /// <value>
        /// The batch begin line number.
        /// </value>
        public int BatchBeginLineNumber { get; }

        /// <summary>
        /// Gets the batch source, either the path to the input file or SQL String if source is a string.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public string Source { get; }

        /// <summary>
        /// Gets the SQL of the batch.
        /// </summary>
        /// <value>
        /// The SQL.
        /// </value>
        public string Sql => this.batch.ToString();

        /// <summary>
        /// Appends the specified text to the internal buffer.
        /// </summary>
        /// <param name="text">The text.</param>
        internal void Append(string text)
        {
            this.batch.Append(text);
        }

        /// <summary>
        /// Appends a new line to the internal buffer.
        /// </summary>
        internal void AppendLine()
        {
            this.batch.AppendLine();
        }

        /// <summary>
        /// Clears the internal buffer.
        /// </summary>
        internal void Clear()
        {
            this.batch.Clear();
        }
    }
}