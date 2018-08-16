namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Matches SETVAR commands
    /// </summary>
    /// <seealso cref="T:SqlExecute.SimpleParser.ICommandMatcher" />
    internal class SetvarCommand : ICommandMatcher
    {
        /// <summary>
        /// The command regex. Quoted values will be returned with the enclosing quotes.
        /// </summary>
        private readonly Regex commandRegex = new Regex(
            @"^\s*:setvar(\s+(?<varname>[^\s\(\)]+)(\s+(?<varvalue>"".*?""|[^\s]+))?)?",
            RegexOptions.IgnoreCase);

        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public CommandType CommandType => CommandType.Setvar;

        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        /// <value>
        /// The name of the variable.
        /// </value>
        public string VarName { get; private set; }

        /// <summary>
        /// Gets the variable value.
        /// </summary>
        /// <value>
        /// The variable value. Will be <c>null</c> if no value provided (i.e. unset)
        /// </value>
        public string VarValue { get; private set; }

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

            this.VarName = m.Groups["varname"].Value;

            // At least the name of the variable is required.
            if (string.IsNullOrEmpty(this.VarName))
            {
                throw new CommandSyntaxException(this.CommandType);
            }

            var val = m.Groups["varvalue"].Value;
            this.VarValue = string.IsNullOrEmpty(val) ? null : val.Unquote();

            return true;
        }
    }
}