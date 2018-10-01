namespace Firefly.SqlCmdParser
{
    using System.IO;

    /// <summary>
    /// Describes how an output file should be created
    /// </summary>
    public interface IOutputFileProperties
    {
        /// <summary>
        /// Gets the mode in which to create/open the file.
        /// </summary>
        /// <value>
        /// The create mode.
        /// </value>
        FileMode CreateMode { get; }

        /// <summary>
        /// Gets the path to the file
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        string Path { get; }
    }
}