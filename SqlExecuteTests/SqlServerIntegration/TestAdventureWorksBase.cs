using System.Diagnostics;

namespace SqlExecuteTests.SqlServerIntegration
{
    using System.Collections.Generic;
    using System.IO;

    using Firefly.SqlCmdParser.Client;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Build the Adventure Works database from the schema published on github using this tool.
    /// </summary>
    /// <typeparam name="T">Version specific SQL Server instance data</typeparam>
    public class TestAdventureWorksBase<T>
        where T : ISqlServerInstanceInfo, new()
    {
        /// <summary>
        ///     Gets the SQL server instance information.
        /// </summary>
        /// <value>
        ///     The SQL server instance information.
        /// </value>
        public ISqlServerInstanceInfo SqlServerInstanceInfo { get; } = new T();

        /// <summary>
        /// Builds the adventure works database from the schema published on github.
        /// This schema exercises many SQL server features.
        /// </summary>
        /// <param name="testContext">The test context.</param>
        public void BuildAdventureWorksOltpDatabase(TestContext testContext)
        {
            // Ensure connection initialised since there's no TestInitialize
            var connection = this.SqlServerInstanceInfo.GetServerConnection();

            Debug.WriteLine(this.SqlServerInstanceInfo);

            if (!this.SqlServerInstanceInfo.FullTextInstalled)
            {
                Assert.Inconclusive("Full Text not supported on this instance.");
            }

            var oltpSchemaDirectory = Path.Combine(TestUtils.AdventureWorksBaseDir, "oltp-install-script");

            if (!Directory.Exists(oltpSchemaDirectory))
            {
                Assert.Inconclusive($"Directory not found: {oltpSchemaDirectory}");
            }


            var variables = new Dictionary<string, string>
                                {
                                    {
                                        "SqlSamplesSourceDataPath",
                                        oltpSchemaDirectory + @"\"
                                    }
                                };

            var initArgs = new TestArguments
                               {
                                   InputFile = Path.Combine(oltpSchemaDirectory, "instawdb.sql"),
                                   ConnectionString =
                                       $"{connection};Application Name={testContext.TestName}",
                                   AbortOnErrorSet = false,
                                   InitialVariables = variables,
                                   OverrideScriptVariablesSet =
                                       true // Override :SETVAR SqlSamplesSourceDataPath in the script with our value.
                               };

            using (var impl = new SqlExecuteImpl(initArgs))
            {
                // Create error proc
                impl.Execute();

                // 2 errors expected when running on localdb
                Assert.AreEqual(2, impl.ErrorCount);
            }
        }
    }
}