namespace Firefly.SqlCmdParser.SimpleParser.Commands
{
    using System.Linq;

    /// <summary>
    /// Extensions to <see cref="string"/> class.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Unquotes the specified string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>String with any surrounding quotes removed</returns>
        public static string Unquote(this string str)
        {
            if (str.Length >= 2 && str.First() == '"' && str.Last() == '"')
            {
                return str.Substring(1, str.Length - 2);
            }

            return str;
        }
    }
}
