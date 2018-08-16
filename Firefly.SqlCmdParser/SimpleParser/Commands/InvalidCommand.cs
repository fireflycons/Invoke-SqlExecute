namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Final match for commands
    /// Matches colon at start of line meaning that if this matches
    /// it's an invalid SQLCMD directive in that it wasn't mathed by anything else
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ICommandMatcher" />
    internal class InvalidCommand : ICommandMatcher
    {
        /// <summary>
        /// The command regex. Quoted values will be returned with the encosing quotes.
        /// </summary>
        private readonly Regex commandRegex = new Regex(@"^\s*:");

        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public CommandType CommandType => CommandType.InvalidCommand;

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
            return this.commandRegex.IsMatch(line);
        }
    }
}