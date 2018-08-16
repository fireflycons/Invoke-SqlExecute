namespace Firefly.SqlCmdParser.SimpleParser
{
    using System;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Tokenizes line data into 'comment' or 'not comment' tokens
    /// </summary>
    internal class Tokenizer
    {
        /// <summary>
        /// The block comment end
        /// </summary>
        private const string BlockCommentEnd = "*/";

        /// <summary>
        /// The block comment start
        /// </summary>
        private const string BlockCommentStart = "/*";

        /// <summary>
        /// The line comment
        /// </summary>
        private const string LineComment = "--";

        /// <summary>
        /// The single quote
        /// </summary>
        private const string SingleQuote = "'";

        /// <summary>
        /// The single quote
        /// </summary>
        private const string QuoteQuote = "''";

        /// <summary>
        /// The double quote
        /// </summary>
        private const string DoubleQuote = "\"";

        /// <summary>
        /// <see cref="StringBuilder"/> used to build up the next token value.
        /// </summary>
        private readonly StringBuilder valueBuilder = new StringBuilder();

        /// <summary>
        /// Input line split into characters.
        /// </summary>
        private char[] lineChars;

        /// <summary>
        /// The current position within <see cref="lineChars"/> buffer.
        /// </summary>
        private int position;

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public TokenizerState State { get; private set; } = TokenizerState.None;

        /// <summary>
        /// Adds the next line of input to the tokenizer.
        /// </summary>
        /// <param name="line">The line.</param>
        public void AddLine(string line)
        {
            this.lineChars = line.ToCharArray();
            this.position = 0;
        }

        /// <summary>
        /// Gets the next token.
        /// </summary>
        /// <returns>
        /// A <see cref="Token" /> or <c>null</c> at end of string.
        /// </returns>
        public Token GetNextToken()
        {
            this.valueBuilder.Clear();
            var previousState = this.State;

            while (this.position < this.lineChars.Length)
            {
                // Peek current position.
                var s1 = new string(this.lineChars[this.position], 1);

                // Peek 2 character string at current position.
                var s2 = new string(
                    this.lineChars.Skip(this.position).Take(Math.Min(2, this.lineChars.Length - this.position))
                        .ToArray());

                switch (this.State)
                {
                    case TokenizerState.None:

                        // Look for start of string literal
                        switch (s1)
                        {
                            case SingleQuote:

                                this.State = TokenizerState.SingleQuoteString;
                                this.Consume();
                                continue;

                            case DoubleQuote:

                                this.State = TokenizerState.DoubleQuoteString;
                                this.Consume();
                                continue;
                        }

                        // Look for start/end of comments
                        switch (s2)
                        {
                            case LineComment:

                                this.State = TokenizerState.LineComment;

                                if (this.position == 0)
                                {
                                    // If at start of line, continue parsing comment
                                    continue;
                                }

                                break;

                            case BlockCommentStart:

                                this.State = TokenizerState.BlockComment;

                                if (this.position == 0)
                                {
                                    // If at start of line, continue parsing comment
                                    continue;
                                }

                                break;

                            case BlockCommentEnd:

                                throw new InvalidOperationException("Found BlockCommentEnd when not within a block comment");

                            default:
                                this.Consume();
                                continue;
                        }

                        break;

                    case TokenizerState.BlockComment:

                        // Look for block comment end
                        if (s2 == BlockCommentEnd)
                        {
                            this.State = TokenizerState.None;
                            this.Consume(2);
                        }
                        else
                        {
                            this.Consume();
                            continue;
                        }

                        break;

                    case TokenizerState.LineComment:

                        // While in a line comment, consume till end of string
                        this.Consume();
                        continue;

                    case TokenizerState.SingleQuoteString:

                        if (s2 == QuoteQuote)
                        {
                            // QuoteQuote within single quote string literal is part of the string
                            this.Consume(2);
                            continue;
                        }

                        if (s1 == SingleQuote)
                        {
                            // End of string literal
                            this.State = TokenizerState.None;
                        }

                        this.Consume();
                        continue;

                    case TokenizerState.DoubleQuoteString:

                        if (s1 == DoubleQuote)
                        {
                            // End of string literal
                            this.State = TokenizerState.None;
                        }

                        this.Consume();
                        continue;
                }

                // If we get here, the token is ready to be emitted
                break;
            }

            var value = this.valueBuilder.ToString();

            if (value.Length == 0)
            {
                // End of line
                if (this.State == TokenizerState.LineComment)
                {
                    // Thus end of line comment
                    this.State = TokenizerState.None;
                }

                return null;
            }

            if (previousState == TokenizerState.BlockComment || previousState == TokenizerState.LineComment)
            {
                return new Token(TokenType.Comment, value);
            }

            return new Token(TokenType.Text, value);
        }

        /// <summary>
        /// Consumes the specified number of chars from input buffer to output buffer.
        /// </summary>
        /// <param name="numChars">The number chars.</param>
        private void Consume(int numChars = 1)
        {
            for (var i = 0; i < numChars; ++i)
            {
                this.valueBuilder.Append(this.lineChars[this.position++]);
            }
        }
    }
}