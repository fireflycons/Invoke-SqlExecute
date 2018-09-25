namespace Firefly.SqlCmdParser
{
    using System;
    using System.Runtime.Serialization;

    using Firefly.SqlCmdParser.SimpleParser;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Exception" />
    /// <inheritdoc />
    [Serializable]
    public class ParserException : Exception
    {
        /// <summary>
        /// The batch source
        /// </summary>
        private readonly IBatchSource batchSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="batchSource">The batch source.</param>
        /// <inheritdoc />
        internal ParserException(TokenizerState state, IBatchSource batchSource)
            : base(FormatTokenizerStateError(state))
        {
            this.batchSource = batchSource;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="batchSource">The batch source.</param>
        /// <inheritdoc />
        internal ParserException(string message, IBatchSource batchSource)
            : base(message)
        {
            this.batchSource = batchSource;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        /// <param name="batchSource">The batch source.</param>
        /// <inheritdoc />
        internal ParserException(string message, Exception inner, IBatchSource batchSource)
            : base(message, inner)
        {
            this.batchSource = batchSource;
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.ParserException" /> class.
        /// </summary>
        // ReSharper disable once UnusedMember.Global - Prevent default construction
        protected ParserException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <inheritdoc />
        protected ParserException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        /// <inheritdoc />
        protected ParserException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <inheritdoc />
        protected ParserException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Creates the invalid tokenizer state exception.
        /// </summary>
        /// <param name="tokenizerState">State of the tokenizer.</param>
        /// <param name="batchSource">The batch source.</param>
        /// <returns>A derived <see cref="ParserException"/> based on the state</returns>
        internal static ParserException CreateInvalidTokenizerStateException(
            TokenizerState tokenizerState,
            IBatchSource batchSource)
        {
            switch (tokenizerState)
            {
                case TokenizerState.SingleQuoteString:
                case TokenizerState.DoubleQuoteString:

                    return new UnclosedStringLiteralException(tokenizerState, batchSource);

                case TokenizerState.BlockComment:

                    return new UnclosedBlockCommentException(tokenizerState, batchSource);

                default:

                    return new ParserException($"Unexpected state {tokenizerState}", batchSource);
            }
        }
        /// <summary>
        /// Gets the filename.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        public string Filename => this.batchSource?.Filename ?? "<None>";

        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>
        /// The line number.
        /// </value>
        public int LineNumber => this.batchSource?.CurrentLineNumber ?? -1;

        /// <summary>
        /// Formats the tokenizer state error.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Formatted string.</returns>
        private static string FormatTokenizerStateError(TokenizerState state)
        {
            switch (state)
            {
                case TokenizerState.BlockComment:

                    return "Unclosed block comment at end of file";

                case TokenizerState.DoubleQuoteString:

                    return "Unclosed double-quote string at end of file";

                case TokenizerState.SingleQuoteString:

                    return "Unclosed single-quote string at end of file";

                default:

                    return $"Unexpected state at end of file (should not be an error): {state}";
            }
        }
    }
}