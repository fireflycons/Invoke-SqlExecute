namespace Firefly.SqlCmdParser.Client
{
    using System.Collections;
    using System.Linq;

    /// <summary>
    /// Methods to check arguments
    /// </summary>
    public static class ArgumentHelpers
    {
        /// <summary>
        /// Determines whether [is empty argument] [the specified argument].
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <returns>
        ///   <c>true</c> if [is empty argument] [the specified argument]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmptyArgument(object argument)
        {
            switch (argument)
            {
                case null:
                case IEnumerable a when !a.Cast<object>().Any():
                case string s when string.IsNullOrEmpty(s):
                    return true;
                default:
                    return false;
            }
        }
    }
}