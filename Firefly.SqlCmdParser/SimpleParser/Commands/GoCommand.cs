namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Matches GO
    /// </summary>
    /// <seealso cref="T:SqlExecute.SimpleParser.ICommandMatcher" />
    internal class GoCommand : ICommandMatcher
    {
        /// <summary>
        /// The go regex
        /// </summary>
        private readonly Regex commandRegex = new Regex(@"^\s*go(\s+(?<num>\d+))?\s*$", RegexOptions.IgnoreCase);

        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public CommandType CommandType => CommandType.Go;

        /// <summary>
        /// Gets the execution count.
        /// </summary>
        /// <value>
        /// The execution count.
        /// </value>
        public int ExecutionCount { get; private set; }

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

            this.ExecutionCount = 1;

            if (!string.IsNullOrEmpty(m.Groups["num"].Value))
            {
                this.ExecutionCount = int.Parse(m.Groups["num"].Value);
            }

            return true;
        }
    }
}