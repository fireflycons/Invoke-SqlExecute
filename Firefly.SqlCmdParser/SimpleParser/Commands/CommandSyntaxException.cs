namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System;
    using System.Runtime.Serialization;

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a syntax error is found parsing a command
    /// </summary>
    /// <seealso cref="T:System.Exception" />
    [Serializable]
    public class CommandSyntaxException : Exception
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.CommandSyntaxException" /> class.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        public CommandSyntaxException(CommandType commandType)
            : base($"Syntax error in {commandType} command")
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.CommandSyntaxException" /> class.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="message">The message.</param>
        public CommandSyntaxException(CommandType commandType, string message)
            : base($"Syntax error in {commandType} command: {message}")
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.CommandSyntaxException" /> class.
        /// </summary>
        protected CommandSyntaxException()
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.CommandSyntaxException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected CommandSyntaxException(string message)
            : base(message)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.CommandSyntaxException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        protected CommandSyntaxException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.SimpleParser.Commands.CommandSyntaxException" /> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected CommandSyntaxException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}