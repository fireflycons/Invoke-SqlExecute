namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System;
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Handles <c>:ON ERROR</c>
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ICommandMatcher" />
    internal class OnErrorCommand : ICommandMatcher
    {
        /// <summary>
        /// The command regex. Quoted values will be returned with the enclosing quotes.
        /// </summary>
        private readonly Regex commandRegex = new Regex(@"^\s*:on\s+error\s+(?<action>\w+)?", RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the error action.
        /// </summary>
        /// <value>
        /// The error action.
        /// </value>
        public ErrorAction ErrorAction { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public CommandType CommandType => CommandType.OnError;

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

            var action = m.Groups["action"].Value;

            // Will throw if action value isn't an enum name
            this.ErrorAction = (ErrorAction)Enum.Parse(typeof(ErrorAction), action, true);

            return true;
        }
    }
}
