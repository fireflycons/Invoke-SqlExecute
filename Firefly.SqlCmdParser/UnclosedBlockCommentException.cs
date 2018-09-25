// ReSharper disable UnusedMember.Global
namespace Firefly.SqlCmdParser
{
    using System;
    using System.Runtime.Serialization;

    using Firefly.SqlCmdParser.SimpleParser;

    /// <summary>
    /// Exception raised when the parser detects an unclosed block comment
    /// </summary>
    /// <seealso cref="Firefly.SqlCmdParser.ParserException" />
    [Serializable]
    public class UnclosedBlockCommentException : ParserException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedBlockCommentException"/> class.
        /// </summary>
        /// <param name="tokenizerState">State of the tokenizer.</param>
        /// <param name="batchSource">The batch source.</param>
        internal UnclosedBlockCommentException(TokenizerState tokenizerState, IBatchSource batchSource)
            : base(tokenizerState, batchSource)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedBlockCommentException"/> class.
        /// </summary>
        /// <inheritdoc />
        protected UnclosedBlockCommentException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedBlockCommentException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <inheritdoc />
        protected UnclosedBlockCommentException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedBlockCommentException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        /// <inheritdoc />
        protected UnclosedBlockCommentException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnclosedBlockCommentException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <inheritdoc />
        protected UnclosedBlockCommentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}