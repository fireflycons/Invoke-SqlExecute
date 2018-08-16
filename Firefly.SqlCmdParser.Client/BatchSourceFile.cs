namespace Firefly.SqlCmdParser.Client
{
    using System.IO;
    using System.Text;

    /// <inheritdoc />
    /// <summary>
    /// Input batch source for SQL in a file
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.IBatchSource" />
    public class BatchSourceFile : IBatchSource
    {
        /// <summary>
        /// The reader
        /// </summary>
        private StreamReader reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchSourceFile"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public BatchSourceFile(string fileName)
        {
            this.Filename = fileName;
            this.reader = new StreamReader(File.OpenRead(fileName), Encoding.UTF8, true);
            this.reader.Peek();
            this.Encoding = this.reader.CurrentEncoding;
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
        public Encoding Encoding { get; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the filename of the input file or <c>SQL String</c>  if the source is a string.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        public string Filename { get; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the next line from the input source
        /// </summary>
        /// <returns>
        /// Next line of input source
        /// </returns>
        /// <exception cref="T:System.IO.EndOfStreamException">Attempt to read past end of file.</exception>
        public string GetNextLine()
        {
            if (this.reader == null)
            {
                // Shouldn't still be trying to read
                throw new EndOfStreamException($"Attempt to read past end of file: {this.Filename}");
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