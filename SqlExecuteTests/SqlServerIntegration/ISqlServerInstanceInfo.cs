namespace SqlExecuteTests.SqlServerIntegration
{
    public interface ISqlServerInstanceInfo
    {
        /// <summary>
        /// Gets the server and authentication parts of the connection string.
        /// </summary>
        /// <value>
        /// The partial connection string.
        /// </value>
        string ServerConnection { get; }
    }
}