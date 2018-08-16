namespace Firefly.SqlCmdParser.SimpleParser
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Default current directory resolver
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.ICurrentDirectoryResolver" />
    internal class NetRuntimeCurrentDirectoryResolver : ICurrentDirectoryResolver
    {
        /// <inheritdoc />
        /// <summary>
        /// Gets the current directory.
        /// </summary>
        /// <returns>
        /// Current directory
        /// </returns>
        public string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }
    }
}