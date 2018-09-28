namespace Firefly.SqlCmdParser.Client
{
    using System.IO;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Firefly.SqlCmdParser.IOutputFileProperties" />
    public class OutputFileProperties : IOutputFileProperties
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutputFileProperties"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="createMode">The create mode.</param>
        public OutputFileProperties(string path, FileMode createMode)
        {
            this.Path = path;
            this.CreateMode = createMode;
        }

        /// <summary>
        /// Gets the mode in which to create/open the file.
        /// </summary>
        /// <value>
        /// The create mode.
        /// </value>
        public FileMode CreateMode { get; }

        /// <summary>
        /// Gets the path to the file
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; }
    }
}