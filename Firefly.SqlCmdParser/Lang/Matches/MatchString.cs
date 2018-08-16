namespace SqlExecute.Lang.Matches
{
    using System;
    using System.Text;

    using SqlExecute.Lang.Data;
    using SqlExecute.Lang.Lexers;

    public class MatchString : MatcherBase
    {
        public const string QUOTE = "\"";

        public const string TIC = "'";

        private String StringDelim { get; set; }

        public MatchString(String delim)
        {
            this.StringDelim = delim;
        }

        protected override Token IsMatchImpl(Tokenizer tokenizer)
        {
            var str = new StringBuilder();

            if (tokenizer.Current == this.StringDelim)
            {
                tokenizer.Consume();

                while (!tokenizer.End() && tokenizer.Current != this.StringDelim)
                {
                    str.Append(tokenizer.Current);
                    tokenizer.Consume();
                }

                if (tokenizer.Current == this.StringDelim)
                {
                    tokenizer.Consume();
                }
            }

            if (str.Length > 0)
            {
                return new Token(TokenType.QuotedString, str.ToString());
            }

            return null;
        }
    }
}
