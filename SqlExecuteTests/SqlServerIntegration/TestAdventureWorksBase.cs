namespace SqlExecuteTests.SqlServerIntegration
{
    using System.Collections.Generic;
    using System.IO;

    using Firefly.SqlCmdParser.Client;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Build the Adventure Works database from the schema published on github using this tool.
    /// </summary>
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
        ///     Builds the adventure works database from the schema published on github.
        ///     This schema exercises many SQL server features.
        /// </summary>
        public void BuildAdventureWorksOltpDatabase(TestContext testContext)
        {
            if (!this.SqlServerInstanceInfo.FullTextInstalled)
            {
                Assert.Inconclusive("Full Text not supported on this instance.");
            }

            var oltpSchemaDirectory = Path.Combine(TestUtils.AdventureWorksSchemaDirectory, "oltp_install_script");

            if (!Directory.Exists(oltpSchemaDirectory))
                Assert.Inconclusive($"Directory not found: {oltpSchemaDirectory}");

            var variables = new Dictionary<string, string>
                                {
                                    {
                                        "SqlSamplesSourceDataPath",
                                        oltpSchemaDirectory + @"\"
                                    },
                                    {
                                        "EnableFullTextFeature",
                                        this.SqlServerInstanceInfo.FullTextInstalled ? "1" : "0"
                                    }
                                };

            var initArgs = new TestArguments
                               {
                                   InputFile = Path.Combine(oltpSchemaDirectory, "instawdb.sql"),
                                   ConnectionString =
                                       $"{this.SqlServerInstanceInfo.GetServerConnection()};Application Name={testContext.TestName}",
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