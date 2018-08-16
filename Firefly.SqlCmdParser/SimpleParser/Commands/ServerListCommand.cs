namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    /// <inheritdoc />
    /// <summary>
    /// Handles <c>:SERVERLIST</c> commmand
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ParameterlessCommand" />
    internal class ServerListCommand : ParameterlessCommand
    {
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        /// <inheritdoc />
        public override CommandType CommandType => CommandType.ServerList;

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