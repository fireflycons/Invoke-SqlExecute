namespace SqlExecuteTests.SqlServerIntegration
{
    using System.Collections.Generic;
    using System.IO;

    using Firefly.SqlCmdParser.Client;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Build the Adventure Works database from the schema published on github using this tool.
    /// </summary>
    [TestClass]
    public class TestAdventureWorks
    {
        /// <summary>
        /// The schema directory
        /// </summary>
        private static string schemaDirectory;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Initialize the test class
        /// </summary>
        /// <param name="context">The context.</param>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            schemaDirectory = TestUtils.UnpackAdventureWorksSchema();
        }

        /// <summary>
        /// Builds the adventure works database from the schema published on github.
        /// This schema exercises many SQL server features.
        /// </summary>
        [TestMethod]
        public void BuildAdventureWorksDatabase()
        {
            if (!Directory.Exists(schemaDirectory))
            {
                Assert.Inconclusive($"Directory not found: {schemaDirectory}");
            }

            // TODO: Detect full text capability of target server.
            var variables = new Dictionary<string, string>
                                {
                                    { "SqlSamplesSourceDataPath", schemaDirectory + @"\" },
                                    { "EnableFullTextFeature", "0" }
                                };

            var initArgs = new TestArguments
                               {
                                   InputFile = Path.Combine(schemaDirectory, "instawdb.sql"),
                                   ConnectionString =
                                       $"{TestUtils.ServerConnection};Application Name={this.TestContext.TestName}",
                                   AbortOnErrorSet = false,
                                   InitialVariables = variables,
                                   OverrideScriptVariablesSet =
                                       true // Override :SETVAR SqlSamplesSourceDataPath in th script with our value.
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