// ReSharper disable InconsistentNaming
// ReSharper disable InheritdocConsiderUsage
namespace SqlExecuteTests.SqlServerIntegration
{
    using System.Data.SqlClient;
    using System.Diagnostics;

    using Firefly.SqlCmdParser.Client;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for instance specific tests
    /// </summary>
    /// <typeparam name="T">SQL Server instance data</typeparam>
    public class StackOverflow33271446TestsBase<T>
        where T : ISqlServerInstanceInfo, new()
    {
        /// <summary>
        /// The insert null in not null column
        /// </summary>
        private const int InsertNullInNotNullColumn = 515;

        /// <summary>
        /// Gets the SQL server instance information.
        /// </summary>
        /// <value>
        /// The SQL server instance information.
        /// </value>
        public ISqlServerInstanceInfo SqlServerInstanceInfo { get; } = new T();

        /// <summary>
        /// Tests the insert does not execute twice on not null violation base.
        /// </summary>
        /// <param name="testContext">The test context.</param>
        public void Test_InsertDoesNotExecuteTwiceOnNotNullViolationBase(TestContext testContext)
        {
            var initArgs = new TestArguments
                               {
                                   Query = TestUtils.LoadSqlResource("RunStackOverflow33271446"),
                                   ConnectionString =
                                       $"{this.SqlServerInstanceInfo.GetServerConnection()};Database={TestUtils.DatabaseName};Application Name={testContext.TestName}",
                                   QueryTimeout = 1,
                                   AbortOnErrorSet = true
                               };

            using (var impl = new SqlExecuteImpl(initArgs))
            {
                try
                {
                    impl.Execute();
                }
                catch (SqlException e)
                {
                    Assert.AreEqual(InsertNullInNotNullColumn, e.Number);
                }
            }

            // Now assert only one row was inserted into table 's'
            Debug.WriteLine("Asserting that only one row was inserted");
            Assert.AreEqual(
                1,
                TestUtils.ExecuteScalar<int>(
                    $"{this.SqlServerInstanceInfo.GetServerConnection()};Database={TestUtils.DatabaseName};Application Name=RESULT_{testContext.TestName}",
                    "select count(*) from s"),
                "More than one row inserted/");
        }
    }
}