namespace SqlExecuteTests.SqlServerIntegration
{
    /// <summary>
    /// Enumeration that indicates the state of a SQL Server instance
    /// </summary>
    public enum InstanceState
    {
        /// <summary>
        /// Instance availability as yet undetermined
        /// </summary>
        Unknown,

        /// <summary>
        /// Instance is available
        /// </summary>
        Available,

        /// <summary>
        /// Instance is unavailable
        /// </summary>
        Unavailable
    }
}