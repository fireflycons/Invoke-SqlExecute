using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SqlExecuteTests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;

    using Firefly.SqlCmdParser.Client;

    [TestClass]
    public class TestAdventureWorks
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        public TestContext TestContext { get; set; }
        [TestMethod]
        public void TestMethod1()
        {
            var path = this.GetThisFilePath();

            if (string.IsNullOrEmpty(path))
            {
                Assert.Inconclusive("Cannot get path to this file. Are you running a release build (where it doesn't work)?");
            }

            var adventureWorksDir = Path.Combine(Path.GetDirectoryName(path), @"Resources\AdventureWorks");

            if (!Directory.Exists(adventureWorksDir))
            {
                Assert.Inconclusive($"Directory not found: {adventureWorksDir}");
            }

            var variables = new Dictionary<string, string>
                                {
                                    { "SqlSamplesSourceDataPath", adventureWorksDir + @"\" },
                                    { "EnableFullTextFeature", "0" }
                                };

            var initArgs = new TestArguments
                               {
                                   InputFile = Path.Combine(adventureWorksDir, "instawdb.sql"),
                                   ConnectionString =
                                       $"Server={TestUtils.ServerName};Application Name={this.TestContext.TestName}",
                                   AbortOnErrorSet = false,
                                   InitialVariables = variables,
                                   OverrideScriptVariablesSet = true // Override :SETVAR SqlSamplesSourceDataPath in th script with our value.
            };

            using (var impl = new SqlExecuteImpl(initArgs))
            {
                // Create error proc
                impl.Execute();

                // 2 errors expected when running on localdb
                Assert.AreEqual(2, impl.ErrorCount);
            }
        }

        private string GetThisFilePath([CallerFilePath] string sourceFilePath = "")
        {
            return sourceFilePath;
        }
    }
}
