namespace SqlExecuteTests.SqlServerIntegration.AppVeyor
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    // ReSharper disable once StyleCop.SA1600
    // ReSharper disable once InconsistentNaming
    public class Test_StackOverflow33271446_Sql2014 : StackOverflow33271446TestsBase<Sql2014InstanceInfo>
    {
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

        // ReSharper disable once InconsistentNaming
        public void Test_InsertDoesNotExecuteTwiceOnNotNullViolation_Sql2014()
        {
            this.Test_InsertDoesNotExecuteTwiceOnNotNullViolationBase(this.TestContext);
        }

        /// <summary>
        /// Test initializer.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            TestUtils.ExecuteNonQuery(
                $"{this.SqlServerInstanceInfo.GetServerConnection()};Application Name=INIT_{this.TestContext.TestName}",
                TestUtils.LoadSqlResource("TestInitialize"));
            TestUtils.ExecuteNonQuery(
                $"{this.SqlServerInstanceInfo.GetServerConnection()};Database={TestUtils.DatabaseName};Application Name=INIT_{this.TestContext.TestName}",
                TestUtils.LoadSqlResource("SetupStackOverflow33271446"));
        }
    }
}