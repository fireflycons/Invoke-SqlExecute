namespace SqlExecuteTests.SqlServerIntegration
{
    public interface ISqlServerInstanceInfo
    {
        /// <summary>
        /// Gets a value indicating whether [full text installed].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [full text installed]; otherwise, <c>false</c>.
        /// </value>
        // ReSharper disable once UnusedMember.Global
        bool FullTextInstalled { get; }

        /// <summary>
        /// Gets the server and authentication parts of the connection string.
        /// </summary>
        /// <returns>
        ///     The partial connection string.
        /// </returns>
        string GetServerConnection();
    }
}