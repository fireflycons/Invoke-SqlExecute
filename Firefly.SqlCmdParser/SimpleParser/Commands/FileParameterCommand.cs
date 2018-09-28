namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Base class for commands that take a filename
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ICommandMatcher" />
    public abstract class FileParameterCommand : ICommandMatcher
    {
        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public abstract CommandType CommandType { get; }

        /// <summary>
        /// Gets the command text (out, perftrace etc.)
        /// </summary>
        /// <value>
        /// The command text.
        /// </value>
        public string CommandText => this.CommandType.ToString();

        /// <summary>
        /// Gets the filename.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        public string Filename { get; private set; }

        /// <summary>
        /// Gets the output destination.
        /// </summary>
        /// <value>
        /// The output destination.
        /// </value>
        public OutputDestination OutputDestination { get; private set; }

        /// <summary>
        /// Gets the command regex.
        /// </summary>
        /// <value>
        /// The command regex.
        /// </value>
        protected Regex CommandRegex =>
            new Regex(@"^\s*:" + this.CommandText + @"(\s+(?<filename>\"".*?\""|[^\s]+))?", RegexOptions.IgnoreCase);

        /// <summary>
        /// Transposes a file name to a node specific one for parallel executions.
        /// </summary>
        /// <param name="nodeNumber">The node number.</param>
        /// <param name="filepath">The file path.</param>
        /// <returns>Converted path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filepath"/> is null</exception>
        public static string GetNodeFilepath(int nodeNumber, string filepath)
        {
            if (filepath == null)
            {
                throw new ArgumentNullException(nameof(filepath));
            }

            if (nodeNumber == 0)
            {
                return filepath;
            }

            var fn = Path.GetFileName(filepath);
            var dir = Path.GetDirectoryName(filepath);

            fn = $"{nodeNumber:D2}-{fn}";

            if (!string.IsNullOrEmpty(dir))
            {
                return Path.Combine(dir, fn);
            }

            return fn;
        }

    /// <summary>
        /// Determines whether the specified line is a match for this command type.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>
        ///   <c>true</c> if the specified line is match; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="CommandSyntaxException">File name argument is missing</exception>
        public bool IsMatch(string line)
        {
            var m = this.CommandRegex.Match(line);

            if (!m.Success)
            {
                return false;
            }

            var fn = m.Groups["filename"].Value;

            if (string.IsNullOrEmpty(fn))
            {
                throw new CommandSyntaxException(this.CommandType);
            }

            this.Filename = fn.Unquote();
            this.OutputDestination = Enum.TryParse<OutputDestination>(fn, true, out var od) ? od : OutputDestination.File;

            return true;
        }
    }
}
