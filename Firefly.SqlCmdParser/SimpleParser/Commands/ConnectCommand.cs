namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <inheritdoc />
    /// <summary>
    /// Handles the <c>:CONNECT</c> command
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.ICommandMatcher" />
    internal class ConnectCommand : ICommandMatcher
    {
        /// <summary>
        /// The command regex
        /// </summary>
        private readonly Regex commandRegex = new Regex(@"^\s*:connect(\s*|\s+[^\s].*)$");

        /// <summary>
        /// The connect line
        /// </summary>
        private string connectLine;

        /// <inheritdoc />
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public CommandType CommandType => CommandType.Connect;

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; private set; }

        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public string Server { get; private set; }

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        public int Timeout { get; private set; }

        /// <summary>
        /// Gets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string Username { get; private set; }

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
            if (!this.commandRegex.IsMatch(line))
            {
                return false;
            }

            this.connectLine = line;
            return true;
        }

        /// <summary>
        /// Resolves the connection parameters.
        /// </summary>
        /// <param name="variableResolver">The variable resolver.</param>
        public void ResolveConnectionParameters(IVariableResolver variableResolver)
        {
            this.Server = null;
            this.Password = null;
            this.Username = null;
            this.Timeout = int.MinValue;

            // Get the arguments
            var args = this.connectLine.Substring(8).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (args.Length == 0)
            {
                // No arguments would indicate reconnecting to the currently connected server.
                return;
            }

            if (args[0].StartsWith("$"))
            {
                this.ConnectWithVariables(args, variableResolver);
            }
            else
            {
                this.ConnectWithParameters(args);
            }

            if (this.Timeout == int.MinValue)
            {
                this.Timeout = int.Parse(variableResolver.ResolveVariable("SQLCMDTIMEOUT"));
            }

            if (this.Username == null && this.Password == null)
            {
                // Integrated security
                return;
            }

            if (this.Username == null)
            {
                this.Username = variableResolver.ResolveVariable("SQLCMDUSER");
            }

            if (this.Password == null)
            {
                this.Password = variableResolver.ResolveVariable("SQLCMDPASSWORD");
            }
        }

        /// <summary>
        /// Gets connection parameters from command line style parameters.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <exception cref="CommandSyntaxException">
        /// Invalid number of arguments.
        /// or
        /// </exception>
        private void ConnectWithParameters(IList<string> args)
        {
            // First argument is always server name
            this.Server = args[0];

            // Remaining arguments are switch value, so there must be an even number of them
            if ((args.Count - 1) % 2 != 0)
            {
                throw new CommandSyntaxException(this.CommandType, "Invalid number of arguments.");
            }

            for (var i = 1; i < args.Count; i += 2)
            {
                switch (args[i])
                {
                    case "-l":

                        this.Timeout = int.Parse(args[i + 1]);
                        break;

                    case "-U":

                        this.Username = args[i + 1];
                        break;

                    case "-P":

                        this.Password = args[i + 1];
                        break;

                    default:

                        throw new CommandSyntaxException(this.CommandType, $"Invalid switch {args[i]}");
                }
            }
        }

        /// <summary>
        /// Gets connection parameters from scripting variables.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="variableResolver">The variable resolver.</param>
        /// <exception cref="CommandSyntaxException">Unable to parse scripting variable.</exception>
        private void ConnectWithVariables(IList<string> args, IVariableResolver variableResolver)
        {
            for (var i = 0; i < Math.Min(3, args.Count); ++i)
            {
                var m = Parser.VariableRegex.Match(args[i]);

                if (!m.Success)
                {
                    throw new CommandSyntaxException(this.CommandType, "Unable to parse scripting variable.");
                }

                var varValue = variableResolver.ResolveVariable(m.Groups["varname"].Value);

                switch (i)
                {
                    case 0:

                        this.Server = varValue;
                        break;

                    case 1:

                        this.Username = varValue;
                        break;

                    case 2:

                        this.Password = varValue;
                        break;
                }
            }
        }
    }
}