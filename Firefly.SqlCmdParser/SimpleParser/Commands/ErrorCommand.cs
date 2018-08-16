namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    /// <inheritdoc />
    /// <summary>
    /// Handles <c>:ERROR</c> command
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.FileParameterCommand" />
    internal class ErrorCommand : FileParameterCommand
    {
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        /// <inheritdoc />
        public override CommandType CommandType => CommandType.Error;
    }
}