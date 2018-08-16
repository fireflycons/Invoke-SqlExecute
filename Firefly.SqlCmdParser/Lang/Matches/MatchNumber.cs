namespace SqlExecute.Lang.Matches
{
    using System;
    using System.Text.RegularExpressions;

    using SqlExecute.Lang.Data;
    using SqlExecute.Lang.Lexers;

    public class MatchNumber : MatcherBase
    {
        protected override Token IsMatchImpl(Tokenizer tokenizer)
        {

            var leftOperand = this.GetIntegers(tokenizer);

            if (leftOperand != null)
            {
                if (tokenizer.Current == ".")
                {
                    tokenizer.Consume();

                    var rightOperand = this.GetIntegers(tokenizer);

                    // found a float
                    if (rightOperand != null)
                    {
                        return new Token(TokenType.Float, leftOperand + "." + rightOperand);
                    }
                }

                return new Token(TokenType.Int, leftOperand);
            }
            
            return null;
        }

        private String GetIntegers(Tokenizer tokenizer)
        {
            var regex = new Regex("[0-9]");

            String num = null;

            while (tokenizer.Current != null && regex.IsMatch(tokenizer.Current))
            {
                num += tokenizer.Current;
                tokenizer.Consume();
            }

            if (num != null)
            {
                return num;
            }

            return null;
            
        }
    }

}
