namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    /// <summary>
    /// Interface for whole line command matchers (SQLCMD commands)
    /// </summary>
    internal interface ICommandMatcher
    {
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        CommandType CommandType { get; }

        /// <summary>
        /// Determines whether the specified line is a match for this command type.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>
        ///   <c>true</c> if the specified line is match; otherwise, <c>false</c>.
        /// </returns>
        bool IsMatch(string line);
    }
}