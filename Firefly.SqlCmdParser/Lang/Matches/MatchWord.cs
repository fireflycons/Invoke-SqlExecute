namespace SqlExecute.Lang.Matches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SqlExecute.Lang.Data;
    using SqlExecute.Lang.Exceptions;
    using SqlExecute.Lang.Lexers;

    public class MatchWord : MatcherBase
    {
        private List<MatchKeyword> SpecialCharacters { get; set; } 
        public MatchWord(IEnumerable<IMatcher> keywordMatchers)
        {
            this.SpecialCharacters = keywordMatchers.Select(i=>i as MatchKeyword).Where(i=> i != null).ToList();
        }

        protected override Token IsMatchImpl(Tokenizer tokenizer)
        {
            String current = null;

            while (!tokenizer.End() && !String.IsNullOrWhiteSpace(tokenizer.Current) && this.SpecialCharacters.All(m => m.Match != tokenizer.Current))
            {
                current += tokenizer.Current;
                tokenizer.Consume();
            }

            if (current == null)
            {
                return null;
            }

            // can't start a word with a special character
            if (this.SpecialCharacters.Any(c => current.StartsWith(c.Match)))
            {
                throw new InvalidSyntax(String.Format("Cannot start a word with a special character {0}", current));
            }

            return new Token(TokenType.Word, current);
        }
    }
}
