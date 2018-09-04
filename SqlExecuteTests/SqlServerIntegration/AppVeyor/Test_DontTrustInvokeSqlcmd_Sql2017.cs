﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlExecuteTests.SqlServerIntegration.LocalDb;

namespace SqlExecuteTests.SqlServerIntegration.AppVeyor
{
    [TestClass]
    public class Test_DontTrustInvokeSqlcmd_Sql2017 : DontTrustInvokeSqlcmdTestsBase<Sql2017InstanceInfo>
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
        public void Test_InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure_Sql2017()
        {
            this.Test_InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure(this.TestContext);
        }

        [TestMethod]
        public void Test_InvokeSqlcmdDoesReturnRaisedErrorIfQueryWasRunInSingleUserMode_Sql2017()
        {
            this.Test_InvokeSqlcmdDoesReturnRaisedErrorIfQueryWasRunInSingleUserMode(this.TestContext);
        }

        [TestMethod]
        public void Test_InvokeSqlcmdReturnsErrorForArithmeticOverflowError_Sql2017()
        {
            this.Test_InvokeSqlcmdReturnsErrorForArithmeticOverflowError(this.TestContext);
        }

        /// <summary>
        /// Test initialize.
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