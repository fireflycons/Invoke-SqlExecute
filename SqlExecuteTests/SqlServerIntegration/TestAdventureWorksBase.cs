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
        /// Builds the adventure works OLTP database from the schema published on github.
        /// This schema exercises many SQL server features.
        /// </summary>
        /// <param name="testContext">The test context.</param>
        public void BuildAdventureWorksOltp(TestContext testContext)
        {
            this.BuildAdventureWorks(testContext, Path.Combine(TestUtils.AdventureWorksBaseDir, "oltp-install-script"), "instawdb.sql");
        }

        /// <summary>
        /// Builds the adventure works Data Warehouse from the schema published on github.
        /// This schema exercises many SQL server features.
        /// </summary>
        /// <param name="testContext">The test context.</param>
        public void BuildAdventureWorksDataWarehouse(TestContext testContext)
        {
            this.BuildAdventureWorks(testContext, Path.Combine(TestUtils.AdventureWorksBaseDir, "data-warehouse-install-script"), "instawdbdw.sql");
        }


        /// <summary>
        /// Builds an adventure works schema.
        /// </summary>
        /// <param name="testContext">The test context.</param>
        /// <param name="schemaDirectory">The schema directory.</param>
        /// <param name="sqlFile">The SQL file.</param>
        private void BuildAdventureWorks(TestContext testContext, string schemaDirectory, string sqlFile)
        {
            // Ensure connection initialised since there's no TestInitialize
            var connection = this.SqlServerInstanceInfo.GetServerConnection();

            Debug.WriteLine(this.SqlServerInstanceInfo);

            if (!this.SqlServerInstanceInfo.FullTextInstalled)
            {
                Assert.Inconclusive("Full Text not supported on this instance.");
            }

            if (!Directory.Exists(schemaDirectory))
            {
                Assert.Inconclusive($"Directory not found: {schemaDirectory}");
            }


            var variables = new Dictionary<string, string>
                                {
                                    {
                                        "SqlSamplesSourceDataPath",
                                        schemaDirectory + @"\"
                                    }
                                };

            var initArgs = new TestArguments
                               {
                                   InputFile = Path.Combine(schemaDirectory, sqlFile),
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

                Assert.AreEqual(0, impl.ErrorCount);
            }
        }
    }
}