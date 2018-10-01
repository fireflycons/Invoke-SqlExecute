namespace Firefly.SqlCmdParser.Client
{
    using System;
    using System.Collections;
    using System.Linq;

    /// <summary>
    /// Methods to check arguments
    /// </summary>
    public static class ArgumentHelpers
    {
        /// <summary>
        /// Describes an argument for debug output.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <returns>Argument description.</returns>
        public static string DescribeArgument(object argument)
        {
            switch (argument)
            {
                case null:

                    return "<NULL>";

                case IEnumerable a when !a.Cast<object>().Any():

                    return "<EMPTY ARRAY>";

                case string s when string.IsNullOrEmpty(s):

                    return "<EMPTY STRING>";

                case string[] sa:

                    return $"String[{sa.Length}]";

                case bool b:

                    return b.ToString();

                default:

                    return argument.GetType().Name;
            }
        }

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

        /// <summary>
        /// Determines whether the given arguments amount to a single run configuration.
        /// </summary>
        /// <param name="connectionStrings">The connection strings.</param>
        /// <param name="inputFiles">The input files.</param>
        /// <returns>
        ///   <c>true</c> if [is single run configuration]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSingleRunConfiguration(string[] connectionStrings, string[] inputFiles)
        {
            return SafeArrayLength(connectionStrings) <= 1 && SafeArrayLength(inputFiles) <= 1;
        }

        /// <summary>
        /// Safely gets the length of an array. If the array is null, then length = 0
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>The array length.</returns>
        private static int SafeArrayLength(Array array)
        {
            return array?.Length ?? 0;
        }
    }
}