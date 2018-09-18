// ReSharper disable StringLiteralTypo
namespace SqlExecuteTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Firefly.SqlCmdParser;
    using Firefly.SqlCmdParser.SimpleParser;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for the tokenizer
    /// </summary>
    [TestClass]
    public class CommentTokenizerTests
    {
        /// <summary>
        /// The lines2
        /// </summary>
        private readonly string[] commentsCorrectlyParsedInRegularTextAndIgnoredInStringLiteralsLines =
            {
                "Donec interdum arcu libero, '--nec'' porttitor' neque dapibus id.",
                "Phasellus eget congue magna. Duis aliquet pulvinar ex, at vulputate arcu gravida quis.",
                "Proin efficitur blandit dolor, ut vestibulum ante egestas quis.",
            };

        /// <summary>
        /// The tokens2
        /// </summary>
        private readonly Queue<TokenType> commentsCorrectlyParsedInRegularTextAndIgnoredInStringLiteralsTokens2 =
            new Queue<TokenType>(
                new[]
                    {
                        TokenType.Text, TokenType.LineBreak, TokenType.Text, TokenType.LineBreak, TokenType.Text,
                        TokenType.LineBreak
                    });

        /// <summary>
        /// The lines1
        /// </summary>
        private readonly string[] commentsCorrectlyParsedInRegularTextLines =
            {
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras quis tellus tempor, --pulvinar lectus eu, pulvinar nisi.",
                "Sed rhoncus /*venenatis sapien vel commodo*/. Nulla aliquam orci ut metus rhoncus, nec rhoncus quam volutpat.",
                "Donec euismod eleifend nisi, sed ultrices mauris lacinia nec. /* Nunc pulvinar laoreet ultrices.",
                "Ut viverra dignissim orci.*/ Vestibulum aliquam quis neque vel lacinia.",
                "Donec malesuada enim ac ex mattis eleifend."
            };

        /// <summary>
        /// The tokens1
        /// </summary>
        private readonly Queue<TokenType> commentsCorrectlyParsedInRegularTextTokens1 = new Queue<TokenType>(
            new[]
                {
                    TokenType.Text, TokenType.Comment, TokenType.LineBreak, TokenType.Text, TokenType.Comment,
                    TokenType.Text, TokenType.LineBreak, TokenType.Text, TokenType.Comment, TokenType.LineBreak,
                    TokenType.Comment, TokenType.Text, TokenType.LineBreak, TokenType.Text, TokenType.LineBreak
                });

        /// <summary>
        /// The lines3
        /// </summary>
        private readonly string[] multiLineStringLiteralIsParsedLines =
            {
                "Donec interdum arcu libero, '--nec'' porttitor neque dapibus id.",
                "Phasellus' eget congue magna. Duis aliquet pulvinar ex, at vulputate arcu gravida quis.",
                "Proin efficitur blandit dolor, ut vestibulum ante egestas quis.",
            };

        /// <summary>
        /// The tokens3
        /// </summary>
        private readonly Queue<TokenType> multiLineStringLiteralIsParsedTokens = new Queue<TokenType>(
            new[]
                {
                    TokenType.Text, TokenType.LineBreak, TokenType.Text, TokenType.LineBreak, TokenType.Text,
                    TokenType.LineBreak
                });

        /// <summary>
        /// The lines6
        /// </summary>
        private readonly string[] unclosedBlockCommentIsErrorLines =
            {
                "Donec interdum arcu libero, '--nec'' porttitor neque dapibus id.",
                "Phasellus' eget /* congue magna. Duis aliquet pulvinar ex, at vulputate arcu gravida quis.",
                "Proin efficitur blandit dolor, ut vestibulum ante egestas quis.",
            };

        /// <summary>
        /// The lines5
        /// </summary>
        private readonly string[] unclosedDoubleQuoteStringLiteralIsErrorLines =
            {
                "Donec interdum arcu libero, \"--nec'' porttitor neque dapibus id.",
                "Phasellus eget congue magna. Duis aliquet pulvinar ex, at vulputate arcu gravida quis.",
                "Proin efficitur blandit dolor, ut vestibulum ante egestas quis.",
            };

        /// <summary>
        /// The lines4
        /// </summary>
        private readonly string[] unclosedSingleQuoteStringLiteralIsErrorLines =
            {
                "Donec interdum arcu libero, '--nec'' porttitor neque dapibus id.",
                "Phasellus eget congue magna. Duis aliquet pulvinar ex, at vulputate arcu gravida quis.",
                "Proin efficitur blandit dolor, ut vestibulum ante egestas quis.",
            };

        /// <summary>
        /// Tests the comments correctly parsed in regular text.
        /// </summary>
        [TestMethod, TestCategory("Parser")]
        public void TestCommentsCorrectlyParsedInRegularText()
        {
            RunTest(this.commentsCorrectlyParsedInRegularTextLines, this.commentsCorrectlyParsedInRegularTextTokens1);
        }

        /// <summary>
        /// Tests the comments correctly parsed in regular text and ignored in string literals.
        /// </summary>
        [TestMethod, TestCategory("Parser")]
        public void TestCommentsCorrectlyParsedInRegularTextAndIgnoredInStringLiterals()
        {
            RunTest(
                this.commentsCorrectlyParsedInRegularTextAndIgnoredInStringLiteralsLines,
                this.commentsCorrectlyParsedInRegularTextAndIgnoredInStringLiteralsTokens2);
        }

        /// <summary>
        /// Tests the multi line string literal is parsed.
        /// </summary>
        [TestMethod, TestCategory("Parser")]
        public void TestMultiLineStringLiteralIsParsed()
        {
            RunTest(this.multiLineStringLiteralIsParsedLines, this.multiLineStringLiteralIsParsedTokens);
        }

        /// <summary>
        /// Tests the unclosed block comment is error.
        /// </summary>
        [TestMethod, TestCategory("Parser")]
        [ExpectedException(typeof(UnclosedBlockCommentException))]
        public void TestUnclosedBlockCommentIsError()
        {
            RunTest(this.unclosedBlockCommentIsErrorLines, null);
        }

        /// <summary>
        /// Tests the unclosed double quote string literal is error.
        /// </summary>
        [TestMethod, TestCategory("Parser")]
        [ExpectedException(typeof(UnclosedStringLiteralException))]
        public void TestUnclosedDoubleQuoteStringLiteralIsError()
        {
            RunTest(this.unclosedDoubleQuoteStringLiteralIsErrorLines, null);
        }

        /// <summary>
        /// Tests the unclosed single quote string literal is error.
        /// </summary>
        [TestMethod, TestCategory("Parser")]
        [ExpectedException(typeof(UnclosedStringLiteralException))]
        public void TestUnclosedSingleQuoteStringLiteralIsError()
        {
            RunTest(this.unclosedSingleQuoteStringLiteralIsErrorLines, null);
        }

        /// <summary>
        /// Runs the test.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="expectedTokenTypes">The expected token types.</param>
        private static void RunTest(IEnumerable<string> lines, Queue<TokenType> expectedTokenTypes)
        {
            var tokenizer = new Tokenizer();

            try
            {
                foreach (var line in lines)
                {
                    Token token;

                    tokenizer.AddLine(line);

                    while ((token = tokenizer.GetNextToken()) != null)
                    {
                        Debug.WriteLine($"Type : {token.TokenType}");
                        Debug.WriteLine($"Value: {token.TokenValue}");

                        if (expectedTokenTypes != null)
                        {
                            Assert.AreEqual(expectedTokenTypes.Dequeue(), token.TokenType);
                        }
                    }

                    Debug.WriteLine("---------------- line break -----------");

                    if (expectedTokenTypes != null)
                    {
                        Assert.AreEqual(expectedTokenTypes.Dequeue(), TokenType.LineBreak);
                    }
                }

                Debug.WriteLine(string.Empty);
                Debug.WriteLine($"Final State: {tokenizer.State}");

                if (tokenizer.State != TokenizerState.None)
                {
                    throw ParserException.CreateInvalidTokenizerStateException(tokenizer.State, null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}