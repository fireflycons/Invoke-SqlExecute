namespace Firefly.SqlCmdParser.SimpleParser
{
    /// <summary>
    /// Token returned by the <see cref="Tokenizer"/>
    /// </summary>
    internal class Token
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        /// <param name="tokenType">Type of the token.</param>
        /// <param name="tokenValue">The token value.</param>
        public Token(TokenType tokenType, string tokenValue)
        {
            this.TokenType = tokenType;
            this.TokenValue = tokenValue;
        }

        /// <summary>
        /// Gets the type of the token.
        /// </summary>
        /// <value>
        /// The type of the token.
        /// </value>
        public TokenType TokenType { get; }

        /// <summary>
        /// Gets the token value.
        /// </summary>
        /// <value>
        /// The token value.
        /// </value>
        public string TokenValue { get;  }
    }
}
