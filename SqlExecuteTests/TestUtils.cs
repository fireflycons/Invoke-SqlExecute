namespace SqlExecuteTests
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SqlExecuteTests.Resources.AdventureWorks;

    /// <summary>
    /// Helper methods for the tests
    /// </summary>
    internal class TestUtils
    {
        /// <summary>
        /// The database name
        /// </summary>
        public const string DatabaseName = "Test1";

        /// <summary>
        /// The resource names
        /// </summary>
        private static readonly string[] ResourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        /// <summary>
        /// The server connection
        /// </summary>
        private static string serverConnection = null;

        /// <summary>
        /// Gets the server connection, which amounts to server name and authentication.
        /// If AppVeyor is detected, test for all known AppVeyor SQL services and return the first found.
        /// Otherwise default to (localdb)\mssqllocaldb;Integrated Security=true
        /// </summary>
        /// <value>
        /// The server name and authentication part of a connection string.
        /// </value>
        /// <exception cref="InvalidOperationException">Unable to determine AppVeyor SQL server environment</exception>
        public static string ServerConnection
        {
            get
            {
                if (serverConnection != null)
                {
                    return serverConnection;
                }

                var appveyor = Environment.GetEnvironmentVariable("APPVEYOR");

                if (appveyor == null)
                {
                    // ReSharper disable once ConvertToConstant.Local
                    serverConnection = @"Server=(localdb)\mssqllocaldb;Integrated Security=true";
                    var version = ExecuteScalar<string>(serverConnection, "SELECT @@VERSION");
                    Debug.WriteLine($"localdb:\n{version}");
                    return serverConnection;
                }

                // Try to detect what SQL server AppVeyor has provided
                foreach (var server in new[] { "SQL2008R2SP2", "SQL2012SP1", "SQL2014", "SQL2016", "SQL2017" })
                {
                    serverConnection = $"Server=(local)\\{server};User ID=sa;Password=Password12!";

                    try
                    {
                        var version = ExecuteScalar<string>(serverConnection, "SELECT @@VERSION");
                        Debug.WriteLine($"AppVeyor SQL Server:\n{version}");
                        return serverConnection;
                    }
                    catch
                    {
                        // Do nothing - next instance type
                    }
                }

                // If we get here, no dice
                throw new InvalidOperationException("Unable to determine AppVeyor SQL server environment");
            }
        }

        /// <summary>
        /// Executes a single batch of SQL directly via a dedicated <see cref="SqlConnection"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="sql">The SQL.</param>
        public static void ExecuteNonQuery(string connectionString, string sql)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlConnection.ClearPool(conn);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Executes a single batch of SQL directly via a dedicated <see cref="SqlConnection" /> and return scalar result.
        /// </summary>
        /// <typeparam name="T">Type to cast scalar result to.</typeparam>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="sql">The SQL.</param>
        /// <returns>
        /// Scalar result
        /// </returns>
        public static T ExecuteScalar<T>(string connectionString, string sql)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlConnection.ClearPool(conn);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = sql;
                    return (T)cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Loads an SQL resource from embedded resources.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns>SQL text.</returns>
        /// <exception cref="FileNotFoundException">Cannot locate embedded resource <paramref name="resourceName"/>.</exception>
        public static string LoadSqlResource(string resourceName)
        {
            if (!resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                resourceName += ".sql";
            }

            var fullResourceName =
                ResourceNames.FirstOrDefault(r => r.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            if (fullResourceName == null)
            {
                throw new FileNotFoundException($"Cannot locate embedded resource {resourceName}");
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            using (var sr =
                new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName)))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Unpacks the adventure works schema.
        /// </summary>
        /// <returns>Folder where the resource files were unpacked to.</returns>
        public static string UnpackAdventureWorksSchema()
        {
            var resourceNamespace = typeof(IAdventureWorksLocator).Namespace;

            Assert.IsNotNull(resourceNamespace, "Unable to retrieve resource namespace");

            var outputFolder = Path.Combine(Path.GetTempPath(), "AdventureWorks");

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            foreach (var resource in ResourceNames.Where(r => r.StartsWith(resourceNamespace)))
            {
                var f = resource.Substring(resourceNamespace.Length + 1);
                var filename = Path.Combine(outputFolder, f);

                using (var fs = new FileStream(filename, FileMode.Create))
                {
                    using (var rs = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                    {
                        Assert.IsNotNull(rs, $"Unable to retrieve resource: {resource}");
                        rs.CopyTo(fs);
                    }
                }
            }

            return outputFolder;
        }
    }
}