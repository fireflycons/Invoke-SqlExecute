namespace Firefly.SqlCmdParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Object that compiles a batch of SQL
    /// </summary>
    [Serializable]
    public class SqlBatch
    {
        /// <summary>
        /// The batch items
        /// </summary>
        private readonly List<SqlBatchItem> batchItems = new List<SqlBatchItem>();

        /// <summary>
        /// Gets the line number within the source that the batch begins at.
        /// </summary>
        /// <value>
        /// The batch begin line number.
        /// </value>
        public int BatchBeginLineNumber
        {
            get
            {
                var begin = this.batchItems.FirstOrDefault();

                return begin?.LineNumber ?? 0;
            }
        }

        /// <summary>
        /// Gets the batch source, either the path to the input file or SQL String if source is a string.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public string Source
        {
            get
            {
                var begin = this.batchItems.FirstOrDefault();

                return begin?.Source.Filename ?? "<NONE>";
            }
        }

        /// <summary>
        /// Gets the SQL of the batch.
        /// </summary>
        /// <value>
        /// The SQL.
        /// </value>
        public string Sql => string.Join(Environment.NewLine, this.batchItems.Select(i => i.Text));

        /// <summary>
        /// Appends the specified text to the internal buffer.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="source">The source.</param>
        internal void Append(string text, IBatchSource source)
        {
            this.batchItems.Add(new SqlBatchItem(text.TrimEnd(Environment.NewLine.ToCharArray()), source));
        }

        /// <summary>
        /// Appends a new line to the internal buffer.
        /// </summary>
        /// <param name="source">The source.</param>
        internal void AppendLine(IBatchSource source)
        {
            this.batchItems.Add(new SqlBatchItem(string.Empty, source));
        }

        /// <summary>
        /// Clears the internal buffer.
        /// </summary>
        internal void Clear()
        {
            this.batchItems.Clear();
        }
    }
}