namespace SqlExecuteTests.SqlServerIntegration
{
    using System.Data.SqlClient;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for tests that use a SQL server
    /// </summary>
    /// <seealso cref="SqlExecuteTests.SqlServerIntegration.ISqlServerInstanceInfo" />
    public abstract class SqlServerInstanceInfoBase : ISqlServerInstanceInfo
    {
        /// <summary>
        /// The instance specific server connection
        /// </summary>
        private string serverConnection;

        /// <inheritdoc />
        /// <summary>
        /// Gets the server and authentication parts of the connection string.
        /// </summary>
        /// <value>
        /// The partial connection string.
        /// </value>
        public string ServerConnection
        {
            get
            {
                if (this.serverConnection != null)
                {
                    return this.serverConnection;
                }

                if (!this.HaveCheckedConnection)
                {
                    // Try the connection
                    try
                    {
                        var version = TestUtils.ExecuteScalar<string>(
                            this.VersionSpecificServerConnection,
                            "SELECT @@VERSION");
                        Debug.WriteLine(version);
                        this.serverConnection = this.VersionSpecificServerConnection;
                        this.HaveCheckedConnection = true;
                        return this.serverConnection;
                    }
                    catch
                    {
                        // Do nothing...
                    }
                }

                var cb = new SqlConnectionStringBuilder(this.VersionSpecificServerConnection);
                Assert.Inconclusive($"{cb.DataSource} - SQL instance not found.");

                // Assert will throw anyway.
                return null;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [have checked connection].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [have checked connection]; otherwise, <c>false</c>.
        /// </value>
        protected abstract bool HaveCheckedConnection { get; set; }

        /// <summary>
        /// Gets the version specific server connection.
        /// </summary>
        /// <value>
        /// The version specific server connection.
        /// </value>
        protected abstract string VersionSpecificServerConnection { get; }
    }
}