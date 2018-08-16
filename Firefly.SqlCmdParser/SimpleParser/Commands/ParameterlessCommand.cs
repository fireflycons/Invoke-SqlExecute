namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Abstract base class for parameterless commands
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ICommandMatcher" />
    internal abstract class ParameterlessCommand : ICommandMatcher
    {
        /// <summary>
        /// Gets the command text (ed, list etc.)
        /// </summary>
        /// <value>
        /// The command text.
        /// </value>
        public string CommandText => this.CommandType.ToString();

        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public abstract CommandType CommandType { get; }

        /// <summary>
        /// Gets a value indicating whether colon before command text is required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [require colon]; otherwise, <c>false</c>.
        /// </value>
        public abstract bool RequireColon { get; }

        /// <summary>
        /// Gets the command regex.
        /// </summary>
        /// <value>
        /// The command regex.
        /// </value>
        protected Regex CommandRegex => this.RequireColon
                                            ? new Regex(
                                                @"^\s*:" + this.CommandText + @"(\s*|\s+[^\s].*)$",
                                                RegexOptions.IgnoreCase)
                                            : new Regex(
                                                @"^\s*:?" + this.CommandText + @"(\s*|\s+[^\s].*)$",
                                                RegexOptions.IgnoreCase);

        /// <inheritdoc />
        /// <summary>
        /// Determines whether the specified line is a match for this command type.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>
        ///   <c>true</c> if the specified line is match; otherwise, <c>false</c>.
        /// </returns>
        public bool IsMatch(string line)
        {
            return this.CommandRegex.IsMatch(line);
        }
    }
}