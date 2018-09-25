namespace Firefly.SqlCmdParser.SimpleParser
{
    /// <summary>
    /// Type of a <see cref="Token"/>
    /// </summary>
    internal enum TokenType
    {
        /// <summary>
        /// Token contains regular text
        /// </summary>
        Text,

        /// <summary>
        /// Token contains a comment
        /// </summary>
        Comment,

        /// <summary>
        /// Not actually returned by the tokenizer.
        /// Used to check line breaks in unit tests.
        /// </summary>
        LineBreak
    }
}