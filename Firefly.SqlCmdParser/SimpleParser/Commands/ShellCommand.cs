namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Handles <c>:!!</c> command
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ICommandMatcher" />
    internal class ShellCommand : ICommandMatcher
    {
        /// <summary>
        /// The go regex
        /// </summary>
        private readonly Regex commandRegex = new Regex(@"^\s*:?!!(\s+(?<cmd>[^\s].*))?$", RegexOptions.IgnoreCase);

        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public CommandType CommandType => CommandType.Shell;

        /// <summary>
        /// Gets the shell command.
        /// </summary>
        /// <value>
        /// The command.
        /// </value>
        public string Command { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Determines whether the specified line is a match for this command type.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>
        ///   <c>true</c> if the specified line is match; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.CommandSyntaxException">Missing command arguments</exception>
        public bool IsMatch(string line)
        {
            var m = this.commandRegex.Match(line);

            if (!m.Success)
            {
                return false;
            }

            var cmd = m.Groups["cmd"].Value.Trim();

            if (string.IsNullOrEmpty(cmd))
            {
                throw new CommandSyntaxException(this.CommandType, "Missing command arguments");
            }

            this.Command = cmd;

            return true;
        }
    }
}
