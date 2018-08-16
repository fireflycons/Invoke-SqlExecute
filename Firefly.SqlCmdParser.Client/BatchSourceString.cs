namespace Firefly.SqlCmdParser.Client
{
    using System.IO;
    using System.Text;

    /// <inheritdoc />
    /// <summary>
    /// Input batch source for SQL in a string variable
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.IBatchSource" />
    internal class BatchSourceString : IBatchSource
    {
        /// <summary>
        /// Reader for the input SQL
        /// </summary>
        private StringReader reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchSourceString" /> class.
        /// </summary>
        /// <param name="str">The string.</param>
        public BatchSourceString(string str)
        {
            this.reader = new StringReader(str);
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the current line number.
        /// </summary>
        /// <value>
        /// The current line number.
        /// </value>
        public int CurrentLineNumber { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the input file's encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        public Encoding Encoding => Encoding.Unicode;

        /// <inheritdoc />
        /// <summary>
        /// Gets the filename of the input file or <c>SQL String</c>  if the source is a string.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        public string Filename => "<Text String>";

        /// <summary>
        /// Gets the next line from the input source
        /// </summary>
        /// <returns>
        /// Next line of input source
        /// </returns>
        /// <inheritdoc />
        public string GetNextLine()
        {
            if (this.reader == null)
            {
                // Shouldn't still be trying to read
                throw new EndOfStreamException("Attempt to read past end of SQL string");
            }

            var line = this.reader.ReadLine();

            if (line != null)
            {
                ++this.CurrentLineNumber;
            }
            else
            {
                this.reader.Dispose();
                this.reader = null;
            }

            return line;
        }
    }
}