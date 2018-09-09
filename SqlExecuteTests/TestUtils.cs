namespace SqlExecuteTests
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;
    using System.Text.RegularExpressions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SqlExecuteTests.Resources.AdventureWorks;

    /// <summary>
    ///     Helper methods for the tests
    /// </summary>
    [TestClass]
    public class TestUtils
    {
        /// <summary>
        /// The adventure works base directory - as downloaded by AppVeyor init.
        /// Cloned by AppVeyor init from <see href="https://github.com/Microsoft/sql-server-samples/tree/master/samples/databases/adventure-works"/>
        /// </summary>
        public const string AdventureWorksBaseDir = @"C:\TestData\sql-server-samples\samples\databases\adventure-works";

        /// <summary>
        ///     The database name
        /// </summary>
        public const string DatabaseName = "Test1";

        /// <summary>
        ///     The resource names
        /// </summary>
        private static readonly string[] ResourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        public static string AdventureWorksSchemaDirectory { get; private set; }

        /// <summary>
        ///     Executes a single batch of SQL directly via a dedicated <see cref="SqlConnection" />.
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
        ///     Executes a single batch of SQL directly via a dedicated <see cref="SqlConnection" /> and return scalar result.
        /// </summary>
        /// <typeparam name="T">Type to cast scalar result to.</typeparam>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="sql">The SQL.</param>
        /// <returns>
        ///     Scalar result
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
        /// Grants everyone file access to adventure works schema.
        /// </summary>
        /// <param name="testContext">The test context.</param>
        [AssemblyInitialize]
        public static void GrantAccessToAdventureWorksSchema(TestContext testContext)
        {
            if (!Directory.Exists(AdventureWorksBaseDir))
            {
                Debug.WriteLine($"Can't find '{AdventureWorksBaseDir}'. AdventureWorks tests are going to fail.");
                return;
            }

            foreach (var d in Directory.EnumerateDirectories(AdventureWorksBaseDir, "*", SearchOption.TopDirectoryOnly))
            {
                GrantAccess(d);
            }
        }

        /// <summary>
        ///     Loads an SQL resource from embedded resources.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns>SQL text.</returns>
        /// <exception cref="FileNotFoundException">Cannot locate embedded resource <paramref name="resourceName" />.</exception>
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
        /// Grants everyone access to the given directory.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        private static void GrantAccess(string fullPath)
        {
            var directoryInfo = new DirectoryInfo(fullPath);
            var directorySecurity = directoryInfo.GetAccessControl();
            directorySecurity.AddAccessRule(
                new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    FileSystemRights.FullControl,
                    InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                    PropagationFlags.NoPropagateInherit,
                    AccessControlType.Allow));
            directoryInfo.SetAccessControl(directorySecurity);
        }
    }
}