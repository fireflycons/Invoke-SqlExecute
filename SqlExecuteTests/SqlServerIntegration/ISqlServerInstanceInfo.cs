namespace SqlExecuteTests.SqlServerIntegration
{
    public interface ISqlServerInstanceInfo
    {
        /// <summary>
        /// Gets the server and authentication parts of the connection string.
        /// </summary>
        /// <returns>
        ///     The partial connection string.
        /// </returns>
        string GetServerConnection();

        /// <summary>
        /// Gets a value indicating whether [full text installed].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [full text installed]; otherwise, <c>false</c>.
        /// </value>
        bool FullTextInstalled { get; }
    }
}