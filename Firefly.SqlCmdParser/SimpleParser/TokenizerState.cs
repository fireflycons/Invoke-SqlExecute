namespace Firefly.SqlCmdParser.SimpleParser
{
    /// <summary>
    /// Internal state of the tokenizer
    /// </summary>
    internal enum TokenizerState
    {
        /// <summary>
        /// Normal state - processing regular text
        /// </summary>
        None,

        /// <summary>
        /// Currently within single quoted string literal
        /// </summary>
        SingleQuoteString,

        /// <summary>
        /// Currently within double quoted string literal
        /// </summary>
        DoubleQuoteString,

        /// <summary>
        /// Currently within line comment
        /// </summary>
        LineComment,

        /// <summary>
        /// Currently within block comment
        /// </summary>
        BlockComment
    }
}