namespace SqlExecute.Lang.Matches
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using SqlExecute.Lang.Data;
    using SqlExecute.Lang.Lexers;

    public class MatchKeyword : MatcherBase
    {
        public string Match { get; set; }

        private TokenType TokenType { get; set; }


        /// <summary>
        /// If true then matching on { in a string like "{test" will match the first cahracter
        /// because it is not space delimited. If false it must be space or special character delimited
        /// </summary>
        public Boolean AllowAsSubString { get; set; }

        public List<MatchKeyword> SpecialCharacters { get; set; } 

        public MatchKeyword(TokenType type, String match)
        {
            this.Match = match;
            this.TokenType = type;
            this.AllowAsSubString = true;
        }

        protected override Token IsMatchImpl(Tokenizer tokenizer)
        {
            foreach (var character in this.Match)
            {
                if (tokenizer.Current == character.ToString(CultureInfo.InvariantCulture))
                {
                    tokenizer.Consume();
                }
                else
                {
                    return null;
                }
            }

            bool found;

            if (!this.AllowAsSubString)
            {
                var next = tokenizer.Current;

                found = String.IsNullOrWhiteSpace(next) || this.SpecialCharacters.Any(character => character.Match == next);
            }
            else
            {
                found = true;
            }

            if (found)
            {
                return new Token(this.TokenType, this.Match);
            }

            return null;
        }
    }
}
