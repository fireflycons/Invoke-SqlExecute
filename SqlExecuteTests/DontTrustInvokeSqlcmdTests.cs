// ReSharper disable InconsistentNaming
// ReSharper disable InheritdocConsiderUsage
namespace SqlExecuteTests
{
    using System;
    using System.Data.SqlClient;

    using Firefly.SqlCmdParser.Client;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// These tests assert that the issues with <c>Invoke-sqlcmd</c> listed on the following site do not affect this implementation.
    /// <see href="https://sqldevelopmentwizard.blogspot.co.uk/2016/12/invoke-sqlcmd-and-error-results.html" />
    /// </summary>
    [TestClass]
    public class DontTrustInvokeSqlcmdTests
    {
        /// <summary>
        /// The arithmetic overflow error
        /// </summary>
        private const int ArithmeticOverflowError = 8115;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Mies the test initialize.
        /// </summary>
        [TestInitialize]
        public void MyTestInitialize()
        {
            TestUtils.ExecuteNonQuery($"{TestUtils.ServerConnection};Application Name=INIT_{this.TestContext.TestName}", TestUtils.LoadSqlResource("TestInitialize"));
        }

        /// <summary>
        /// Tests the invoke SQLCMD does not return sp name nor line when error occurs in procedure.
        /// </summary>
        [TestMethod]
        public void Test_InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure()
        {
            var initArgs = new TestArguments
                               {
                                   Query = TestUtils.LoadSqlResource(
                                       "InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure"),
                                   ConnectionString =
                                       $"{TestUtils.ServerConnection};Database={TestUtils.DatabaseName};Application Name=1_{this.TestContext.TestName}"
                               };

            using (var impl = new SqlExecuteImpl(initArgs))
            {
                // Create error proc
                impl.Execute();
            }

            initArgs.Query = "EXEC dbo.geterror";
            initArgs.ConnectionString =
                $"{TestUtils.ServerConnection};Database={TestUtils.DatabaseName};Application Name=2_{this.TestContext.TestName}";

            using (var impl = new SqlExecuteImpl(initArgs))
            {
                try
                {
                    // Execute error proc
                    impl.Execute();
                }
                catch (Exception e)
                {
                    if (e.InnerException is SqlException sqlException)
                    {
                        Assert.AreEqual("geterror", sqlException.Procedure);
                        return;
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Tests the invoke SQLCMD does return raised error if query was run in single user mode.
        /// </summary>
        [TestMethod]
        public void Test_InvokeSqlcmdDoesReturnRaisedErrorIfQueryWasRunInSingleUserMode()
        {
            var initArgs = new TestArguments
                               {
                                   Query = TestUtils.LoadSqlResource(
                                       "InvokeSqlcmdDoesNotReturnRaisedErrorIfQueryWasRunInSingleUserMode"),
                                   ConnectionString =
                                       $"{TestUtils.ServerConnection};Application Name={this.TestContext.TestName}"
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
                        Assert.AreEqual("First Error.", sqlException.Message);
                        return;
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Tests the invoke SQLCMD returns error for arithmetic overflow error.
        /// </summary>
        [TestMethod]
        public void Test_InvokeSqlcmdReturnsErrorForArithmeticOverflowError()
        {
            var initArgs = new TestArguments
                               {
                                   Query = "SELECT convert(int,100000000000)",
                                   ConnectionString =
                                       $"{TestUtils.ServerConnection};Application Name={this.TestContext.TestName}"
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
                        Assert.AreEqual(ArithmeticOverflowError, sqlException.Number);
                    }

                    throw;
                }
            }
        }
    }
}