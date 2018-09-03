namespace SqlExecuteTests.SqlServerIntegration.LocalDb
{
    /// <inheritdoc />
    /// <summary>
    /// Instance connection info for SQL 2016 local DB
    /// </summary>
    /// <seealso cref="T:SqlExecuteTests.SqlServerIntegration.SqlServerInstanceInfoBase" />
    public class LocalDb2016InstanceInfo : SqlServerInstanceInfoBase
    {
        /// <summary>
        /// The server connection
        /// </summary>
        // ReSharper disable once StyleCop.SA1309
        // ReSharper disable once InconsistentNaming
        private const string _ServerConnection = @"Server=(localdb)\mssqllocaldb;Integrated Security=true";

        /// <summary>
        /// The have checked connection
        /// </summary>
        private static bool haveCheckedConnection = false;

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets a value indicating whether [have checked connection].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [have checked connection]; otherwise, <c>false</c>.
        /// </value>
        protected override bool HaveCheckedConnection
        {
            get => haveCheckedConnection;

            set => haveCheckedConnection = value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the version specific server connection.
        /// </summary>
        /// <value>
        /// The version specific server connection.
        /// </value>
        protected override string VersionSpecificServerConnection => _ServerConnection;

    }
}
