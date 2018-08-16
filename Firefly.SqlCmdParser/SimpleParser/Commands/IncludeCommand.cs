namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    /// <summary>
    /// Matches <c>:R</c> command
    /// </summary>
    /// <seealso cref="Firefly.SqlCmdParser.SimpleParser.Commands.FileParameterCommand" />
    /// <inheritdoc />
    internal class IncludeCommand : FileParameterCommand
    {
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        /// <inheritdoc />
        public override CommandType CommandType => CommandType.R;
    }
}