namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    /// <summary>
    /// Handles <c>:LIST</c> command
    /// </summary>
    /// <seealso cref="ParameterlessCommand" />
    /// <inheritdoc />
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ParameterlessCommand" />
    internal class ListCommand : ParameterlessCommand
    {
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        /// <inheritdoc />
        public override CommandType CommandType => CommandType.List;

        /// <inheritdoc />
        /// <summary>
        /// Gets a value indicating whether colon before command text is required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [require colon]; otherwise, <c>false</c>.
        /// </value>
        public override bool RequireColon => true;
    }
}
