namespace SqlExecuteTests.SqlServerIntegration.LocalDb
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Test_DontTrustInvokeSqlcmd_LocalDB2016 : DontTrustInvokeSqlcmdTestsBase<LocalDb2016InstanceInfo>
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
        public void Test_InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure_LocalDB2016()
        {
            this.Test_InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure(this.TestContext);
        }

        [TestMethod]
        public void Test_InvokeSqlcmdDoesReturnRaisedErrorIfQueryWasRunInSingleUserMode_LocalDB2016()
        {
            this.Test_InvokeSqlcmdDoesReturnRaisedErrorIfQueryWasRunInSingleUserMode(this.TestContext);
        }

        [TestMethod]
        public void Test_InvokeSqlcmdReturnsErrorForArithmeticOverflowError_LocalDB2016()
        {
            this.Test_InvokeSqlcmdReturnsErrorForArithmeticOverflowError(this.TestContext);
        }

        /// <summary>
        /// Mies the test initialize.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            TestUtils.ExecuteNonQuery(
                $"{this.SqlServerInstanceInfo.ServerConnection};Application Name=INIT_{this.TestContext.TestName}",
                TestUtils.LoadSqlResource("TestInitialize"));
        }
    }
}