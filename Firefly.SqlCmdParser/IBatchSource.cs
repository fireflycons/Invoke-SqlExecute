namespace Firefly.SqlCmdParser
{
    using System.Text;

    /// <summary>
    /// Interface that defines input source for SQL
    /// </summary>
    public interface IBatchSource
    {
        /// <summary>
        /// Gets the current line number.
        /// </summary>
        /// <value>
        /// The current line number.
        /// </value>
        int CurrentLineNumber { get; }

        /// <summary>
        /// Gets the input file's encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        Encoding Encoding { get; }

        /// <summary>
        /// Gets the filename of the input file or <c>SQL String</c>  if the source is a string.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        string Filename { get; }

        /// <summary>
        /// Gets the next line from the input source
        /// </summary>
        /// <returns>Next line of input source</returns>
        /// <exception cref="T:System.IO.EndOfStreamException">Attempt to read past end of file/string.</exception>
        string GetNextLine();
    }
}