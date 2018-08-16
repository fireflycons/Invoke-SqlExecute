namespace SqlExecute.Lang.Data
{
    using System;

    public class Token
    {
        public TokenType TokenType { get; private set; }

        public String TokenValue { get; private set; }

        public Token(TokenType tokenType, String token)
        {
            this.TokenType = tokenType;
            this.TokenValue = token;
        }

        public Token(TokenType tokenType)
        {
            this.TokenValue = null;
            this.TokenType = tokenType;
        }

        public override string ToString()
        {
            return this.TokenType + ": " + this.TokenValue;
        }
    }
}
