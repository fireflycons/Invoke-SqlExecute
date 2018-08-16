﻿namespace SqlExecute.Lang.Lexers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SqlExecute.Lang.Data;
    using SqlExecute.Lang.Matches;

    public class Lexer
    {
        private Tokenizer Tokenizer { get; set; }

        private List<IMatcher> Matchers { get; set; } 

        public Lexer(String source)
        {
            this.Tokenizer = new Tokenizer(source);
        }

        public IEnumerable<Token> Lex()
        {
            this.Matchers = this.InitializeMatchList();

            var current = this.Next();

            while (current != null && current.TokenType != TokenType.EOF)
            {
                // skip whitespace
                if (current.TokenType != TokenType.WhiteSpace)
                {
                    yield return current;
                }

                current = this.Next();
            }
        }

        private List<IMatcher> InitializeMatchList()
        {
            // the order here matters because it defines token precedence

            var matchers = new List<IMatcher>(64);

            var keywordmatchers = new List<IMatcher> 
                                  {
                                      new MatchKeyword(TokenType.Void, "void"),
                                      new MatchKeyword(TokenType.Int, "int"),
                                      new MatchKeyword(TokenType.Fun, "fun"),
                                      new MatchKeyword(TokenType.If, "if"),
                                      new MatchKeyword(TokenType.Infer, "var"),
                                      new MatchKeyword(TokenType.Else, "else"),
                                      new MatchKeyword(TokenType.While, "while"),
                                      new MatchKeyword(TokenType.For, "for"),
                                      new MatchKeyword(TokenType.Return, "return"),
                                      new MatchKeyword(TokenType.Print, "print"),
                                      new MatchKeyword(TokenType.True, "true"),
                                      new MatchKeyword(TokenType.False, "false"),
                                      new MatchKeyword(TokenType.Boolean, "bool"),
                                      new MatchKeyword(TokenType.String, "string"),
                                      new MatchKeyword(TokenType.Method, "method"),
                                      new MatchKeyword(TokenType.Class, "class"),
                                      new MatchKeyword(TokenType.New, "new"),
                                      new MatchKeyword(TokenType.Nil, "nil"),
                                      new MatchKeyword(TokenType.Try, "try"),
                                      new MatchKeyword(TokenType.Catch, "catch")
                                  };


            var specialCharacters = new List<IMatcher>
                                    {
                                        new MatchKeyword(TokenType.DeRef, "->"),
                                        new MatchKeyword(TokenType.LBracket, "{"),
                                        new MatchKeyword(TokenType.RBracket, "}"),
                                        new MatchKeyword(TokenType.LSquareBracket, "["),
                                        new MatchKeyword(TokenType.RSquareBracket, "]"),
                                        new MatchKeyword(TokenType.Plus, "+"),
                                        new MatchKeyword(TokenType.Minus, "-"),
                                        new MatchKeyword(TokenType.NotCompare, "!="),
                                        new MatchKeyword(TokenType.Compare, "=="),
                                        new MatchKeyword(TokenType.Equals, "="),
                                        new MatchKeyword(TokenType.HashTag, "#"),
                                        new MatchKeyword(TokenType.Comma, ","),
                                        new MatchKeyword(TokenType.OpenParenth, "("),
                                        new MatchKeyword(TokenType.CloseParenth, ")"),
                                        new MatchKeyword(TokenType.Asterix, "*"),
                                        new MatchKeyword(TokenType.Slash, "/"),
                                        new MatchKeyword(TokenType.Carat, "^"),
                                        new MatchKeyword(TokenType.Ampersand, "&"),
                                        new MatchKeyword(TokenType.GreaterThan, ">"),
                                        new MatchKeyword(TokenType.LessThan, "<"),
                                        new MatchKeyword(TokenType.Or, "||"),
                                        new MatchKeyword(TokenType.SemiColon, ";"),
                                        new MatchKeyword(TokenType.Dot, "."),
                                    };

            // give each keyword the list of possible delimiters and not allow them to be 
            // substrings of other words, i.e. token fun should not be found in string "function"
            keywordmatchers.ForEach(keyword =>
                {
                    var current = (keyword as MatchKeyword);
                    current.AllowAsSubString = false;
                    current.SpecialCharacters = specialCharacters.Select(i => i as MatchKeyword).ToList();
                });

            matchers.Add(new MatchString(MatchString.QUOTE));
            matchers.Add(new MatchString(MatchString.TIC));
            matchers.AddRange(specialCharacters);
            matchers.AddRange(keywordmatchers);
            matchers.AddRange(new List<IMatcher>
                                                {
                                                    new MatchWhiteSpace(),
                                                    new MatchNumber(),
                                                    new MatchWord(specialCharacters)
                                                });

            return matchers;
        }

        private Token Next()
        {
            if (this.Tokenizer.End())
            {
                return new Token(TokenType.EOF);
            }

            return 
                 (from match in this.Matchers
                 let token = match.IsMatch(this.Tokenizer)
                 where token != null
                 select token).FirstOrDefault();
        }
    }
}
