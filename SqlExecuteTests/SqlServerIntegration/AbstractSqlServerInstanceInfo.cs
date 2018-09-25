using System.Text;

namespace SqlExecuteTests.SqlServerIntegration
{
    using System.Data.SqlClient;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for tests that use a SQL server
    /// </summary>
    /// <seealso cref="SqlExecuteTests.SqlServerIntegration.ISqlServerInstanceInfo" />
    public abstract class AbstractSqlServerInstanceInfo : ISqlServerInstanceInfo
    {
        /// <inheritdoc />
        /// <summary>
        /// Gets the server and authentication parts of the connection string.
        /// </summary>
        /// <returns>
        ///     The partial connection string.
        /// </returns>
        public string GetServerConnection()
        {
            var cb = new SqlConnectionStringBuilder(this.VersionSpecificServerConnection);

            switch (this.InstanceState)
            {
                case InstanceState.Unknown:

                    // Try the connection
                    try
                    {
                        this.VersionString = TestUtils.ExecuteScalar<string>(
                            this.VersionSpecificServerConnection,
                            "SELECT @@VERSION");

                        this.InstanceState = InstanceState.Available;

                        this.FullTextInstalled = TestUtils.ExecuteScalar<int>(this.VersionSpecificServerConnection,
                                                     "SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')") != 0;

                        Debug.WriteLine(this.ToString());
                        return this.VersionSpecificServerConnection;
                    }
                    catch
                    {
                        this.InstanceState = InstanceState.Unavailable;
                    }

                    Assert.Inconclusive($"{cb.DataSource} - SQL instance not found.");
                    break;

                case InstanceState.Available:

                    Debug.WriteLine(this.ToString());
                    return this.VersionSpecificServerConnection;

                case InstanceState.Unavailable:

                    Assert.Inconclusive($"{cb.DataSource} - SQL instance not found.");
                    break;
            }

            // Won't get here as Assert will have thrown.
            return null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Class: {this.GetType().Name}")
                .AppendLine($"Instance {this.GetHashCode():X8}")
                .AppendLine($"State: {this.InstanceState}");

            if (!string.IsNullOrEmpty(this.VersionString))
            {
                sb.AppendLine($"Version: {this.VersionString}")
                    .AppendLine($"Full Text: {this.FullTextInstalled}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a value indicating whether [full text installed].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [full text installed]; otherwise, <c>false</c>.
        /// </value>
        public abstract bool FullTextInstalled { get; protected set; }

        /// <summary>
        /// Gets or sets the state of the instance represented by the derived class.
        /// </summary>
        /// <value>
        /// The state of the instance.
        /// </value>
        protected abstract InstanceState InstanceState { get; set; }

        /// <summary>
        /// Gets the version specific server connection.
        /// </summary>
        /// <value>
        /// The version specific server connection.
        /// </value>
        protected abstract string VersionSpecificServerConnection { get; }

        protected abstract string VersionString { get; set; }
    }
}