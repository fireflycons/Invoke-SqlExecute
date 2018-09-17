using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlExecuteTests.SqlServerIntegration.LocalDb;

namespace SqlExecuteTests.SqlServerIntegration.AppVeyor
{
    [TestClass]
    public class Test_DontTrustInvokeSqlcmd_Sql2014 : DontTrustInvokeSqlcmdTestsBase<Sql2014InstanceInfo>
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        public TestContext TestContext { get; set; }

        [TestMethod, TestCategory("Integration")]
        public void Should_report_stored_procedure_details_in_error_raised_within_an_executing_procedure_Sql2014()
        {
            this.Should_report_stored_procedure_details_in_error_raised_within_an_executing_procedure(this.TestContext);
        }

        [TestMethod, TestCategory("Integration")]
        public void Should_correctly_RAISERROR_when_database_set_to_single_user_mode_Sql2014()
        {
            this.Should_correctly_RAISERROR_when_database_set_to_single_user_mode(this.TestContext);
        }

        [TestMethod, TestCategory("Integration")]
        public void Should_RAISERROR_on_arithmetic_overflow_Sql2014()
        {
            this.Should_RAISERROR_on_arithmetic_overflow(this.TestContext);
        }

        /// <summary>
        /// Mies the test initialize.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            TestUtils.ExecuteNonQuery(
                $"{this.SqlServerInstanceInfo.GetServerConnection()};Application Name=INIT_{this.TestContext.TestName}",
                TestUtils.LoadSqlResource("TestInitialize"));
        }
    }
}