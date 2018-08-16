namespace SqlExecuteTests
{
    using System;
    using System.CodeDom;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Reflection;

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
        /// The server name
        /// </summary>
        public const string ServerName = @"(localdb)\mssqllocaldb";

        /// <summary>
        /// The resource names
        /// </summary>
        private static readonly string[] ResourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

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

        public static string UnpackAdventureWorksSchema()
        {
            var resourceNamespace = typeof(IAdventureWorksLocator).Namespace;
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
                        rs.CopyTo(fs);
                    }
                }
            }

            return outputFolder;
        }
    }
}