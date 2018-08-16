// ReSharper disable InconsistentNaming
// ReSharper disable InheritdocConsiderUsage
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SqlExecuteTests
{
    using System.Data.SqlClient;
    using System.Diagnostics;

    using Firefly.SqlCmdParser.Client;

    [TestClass]
    public class StackOverflow33271446Tests
    {
        private const int InsertNullInNotNullColumn = 515;

        [TestInitialize]
        public void TestInitialize()
        {
            TestUtils.ExecuteNonQuery($"Server={TestUtils.ServerName};Application Name=INIT_{this.TestContext.TestName}", TestUtils.LoadSqlResource("TestInitialize"));
            TestUtils.ExecuteNonQuery($"Server={TestUtils.ServerName};Database={TestUtils.DatabaseName};Application Name=INIT_{this.TestContext.TestName}", TestUtils.LoadSqlResource("SetupStackOverflow33271446"));
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// <see href="https://stackoverflow.com/questions/33271446/invoke-sqlcmd-runs-script-twice/"/>
        /// </summary>
        [TestMethod]
        public void Test_InsertDoesNotExecuteTwiceOnNotNullViolation()
        {
            var initArgs = new TestArguments
                               {
                                   Query = TestUtils.LoadSqlResource(
                                       "RunStackOverflow33271446"),
                                   ConnectionString =
                                       $"Server={TestUtils.ServerName};Database={TestUtils.DatabaseName};Application Name={this.TestContext.TestName}",
                                   QueryTimeout = 1,
                                   AbortOnErrorSet = true
                               };

            using (var impl = new SqlExecuteImpl(initArgs))
            {
                try
                {
                    impl.Execute();
                }
                catch (Exception e)
                {
                    if (e.InnerException is SqlException sqlException)
                    {
                        Assert.AreEqual(InsertNullInNotNullColumn, sqlException.Number);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Now assert only one row was inserted into table 's'
            Debug.WriteLine("Asserting that only one row was inserted");
            Assert.AreEqual(1, TestUtils.ExecuteScalar<int>($"Server={TestUtils.ServerName};Database={TestUtils.DatabaseName};Application Name=RESULT_{this.TestContext.TestName}", "select count(*) from s"), "More than one row inserted/");
        }
    }
}
