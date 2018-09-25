namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Handles <c>:EXIT</c> command
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ICommandMatcher" />
    internal class ExitCommand : ICommandMatcher
    {
        /// <summary>
        /// The command regex
        /// </summary>
        private readonly Regex commandRegex = new Regex(
            @"^\s*:?exit\s*(?<expr>\((?<query>.*)\)\s*)?$",
            RegexOptions.IgnoreCase);

        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public CommandType CommandType => CommandType.Exit;

        /// <summary>
        /// Gets the exit batch.
        /// </summary>
        /// <value>
        /// The exit batch.
        /// </value>
        public string ExitBatch { get; private set; }

        /// <summary>
        /// Gets a value indicating whether [exit immediately].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [exit immediately]; otherwise, <c>false</c>.
        /// </value>
        public bool ExitImmediately { get; private set; }

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
            var m = this.commandRegex.Match(line);

            if (!m.Success)
            {
                return false;
            }

            this.ExitImmediately = string.IsNullOrEmpty(m.Groups["expr"].Value);

            if (!this.ExitImmediately)
            {
                this.ExitBatch = m.Groups["query"].Value.Trim();
            }

            return true;
        }
    }
}