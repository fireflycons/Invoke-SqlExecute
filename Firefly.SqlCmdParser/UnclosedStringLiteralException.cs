namespace Firefly.SqlCmdParser
{
    using System;
    using System.Runtime.Serialization;

    using Firefly.SqlCmdParser.SimpleParser;

    /// <summary>
    /// Exception raised when the parser detects an unclosed string literal
    /// </summary>
    /// <seealso cref="Firefly.SqlCmdParser.ParserException" />
    [Serializable]
    public class UnclosedStringLiteralException : ParserException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedStringLiteralException"/> class.
        /// </summary>
        /// <param name="tokenizerState">State of the tokenizer.</param>
        /// <param name="batchSource">The batch source.</param>
        internal UnclosedStringLiteralException(TokenizerState tokenizerState, IBatchSource batchSource)
            : base(tokenizerState, batchSource)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedStringLiteralException"/> class.
        /// </summary>
        /// <inheritdoc />
        // ReSharper disable once UnusedMember.Global
        protected UnclosedStringLiteralException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedStringLiteralException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <inheritdoc />
        // ReSharper disable once UnusedMember.Global
        protected UnclosedStringLiteralException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedStringLiteralException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        /// <inheritdoc />
        // ReSharper disable once UnusedMember.Global
        protected UnclosedStringLiteralException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedStringLiteralException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <inheritdoc />
        protected UnclosedStringLiteralException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}