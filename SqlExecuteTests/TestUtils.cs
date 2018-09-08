using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlExecuteTests.Resources.AdventureWorks;

namespace SqlExecuteTests
{
    /// <summary>
    ///     Helper methods for the tests
    /// </summary>
    [TestClass]
    public class TestUtils
    {
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
                    return (T) cmd.ExecuteScalar();
                }
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
            if (!resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) resourceName += ".sql";

            var fullResourceName =
                ResourceNames.FirstOrDefault(r => r.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            if (fullResourceName == null)
                throw new FileNotFoundException($"Cannot locate embedded resource {resourceName}");

            // ReSharper disable once AssignNullToNotNullAttribute
            using (var sr =
                new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName)))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        ///     Unpacks the adventure works schema as the assembly initialize method
        /// </summary>
        /// <returns>Folder where the resource files were unpacked to.</returns>
        [AssemblyInitialize]
        public static void UnpackAdventureWorksSchema(TestContext testContext)
        {
            var resourceNamespace = typeof(IAdventureWorksLocator).Namespace;

            Assert.IsNotNull(resourceNamespace, "Unable to retrieve resource namespace");

            var outputFolder = Path.Combine(Path.GetTempPath(), "AdventureWorks");

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            var asciiEncoding = new ASCIIEncoding();
            var ucs2leEncoding = new UnicodeEncoding(false, true);
            
            foreach (var resource in ResourceNames.Where(r => r.StartsWith(resourceNamespace)))
            {
                var f = Regex.Replace(resource.Substring(resourceNamespace.Length + 1), @"\.(?=.*?.\.)", @"\");

                var relativePath = Path.GetDirectoryName(f);

                if (!string.IsNullOrEmpty(relativePath))
                {
                    var d = Path.Combine(outputFolder, relativePath);

                    if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                }

                var pathname = Path.Combine(outputFolder, f);

                using (var rs = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Assert.IsNotNull(rs, $"Unable to retrieve resource: {resource}");

                    Encoding enc;

                    if (Path.GetExtension(pathname) == ".sql")
                    {
                        enc = ucs2leEncoding;
                    }
                    else
                    {
                        enc = asciiEncoding;
                    }

                    using (var sr = new StreamReader(rs, enc))
                    {
                        using (var sw = new StreamWriter(pathname, false, enc))
                        {
                            while (sr.Peek() >= 0)
                            {
                                var line = sr.ReadLine();
                                sw.WriteLine(line);
                            }
                        }
                    }
                }

                // So SQL server can read the file
                GrantAccess(pathname);
            }

            AdventureWorksSchemaDirectory = outputFolder;
        }

        private static void GrantAccess(string fullPath)
        {
            var dInfo = new DirectoryInfo(fullPath);
            var dSecurity = dInfo.GetAccessControl();
            dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            dInfo.SetAccessControl(dSecurity);
        }
    }
}