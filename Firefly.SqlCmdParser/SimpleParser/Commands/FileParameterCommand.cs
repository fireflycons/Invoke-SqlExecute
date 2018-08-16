namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System;
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Base class for commands that take a filename
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ICommandMatcher" />
    internal abstract class FileParameterCommand : ICommandMatcher
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
        protected Regex CommandRegex => new Regex(
            @"^\s*:" + this.CommandText + @"(\s+(?<filename>\"".*?\""|[^\s]+))?",
            RegexOptions.IgnoreCase);

        /// <summary>
        /// Determines whether the specified line is a match for this command type.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>
        ///   <c>true</c> if the specified line is match; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="CommandSyntaxException"></exception>
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
