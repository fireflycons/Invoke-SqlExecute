namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    /// <inheritdoc />
    /// <summary>
    /// Handles <c>:OUT</c> command
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.FileParameterCommand" />
    internal class OutCommand : FileParameterCommand
    {
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        /// <inheritdoc />
        public override CommandType CommandType => CommandType.Out;
    }
}