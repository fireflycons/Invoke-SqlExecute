// ReSharper disable InconsistentNaming
// ReSharper disable InheritdocConsiderUsage
namespace SqlExecuteTests.SqlServerIntegration
{
    using System;
    using System.Data.SqlClient;

    using Firefly.SqlCmdParser.Client;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// These tests assert that the issues with <c>Invoke-sqlcmd</c> listed on the following site do not affect this implementation.
    /// <see href="https://sqldevelopmentwizard.blogspot.co.uk/2016/12/invoke-sqlcmd-and-error-results.html" />
    /// </summary>
    public class DontTrustInvokeSqlcmdTestsBase<T> where T : ISqlServerInstanceInfo, new()
    {
        /// <summary>
        /// SQL server error code for arithmetic overflow
        /// </summary>
        private const int ArithmeticOverflowError = 8115;

        /// <summary>
        /// Gets the SQL server instance information.
        /// </summary>
        /// <value>
        /// The SQL server instance information.
        /// </value>
        public T SqlServerInstanceInfo { get; } = new T();

        /// <summary>
        /// Tests the invoke SQLCMD does not return sp name nor line when error occurs in procedure.
        /// </summary>
        public void Test_InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure(TestContext testContext)
        {
            var initArgs = new TestArguments
                               {
                                   Query = TestUtils.LoadSqlResource(
                                       "InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure"),
                                   ConnectionString =
                                       $"{this.SqlServerInstanceInfo.ServerConnection};Database={TestUtils.DatabaseName};Application Name=1_{testContext.TestName}"
                               };

            using (var impl = new SqlExecuteImpl(initArgs))
            {
                // Create error proc
                impl.Execute();
            }

            initArgs.Query = "EXEC dbo.geterror";
            initArgs.ConnectionString =
                $"{this.SqlServerInstanceInfo.ServerConnection};Database={TestUtils.DatabaseName};Application Name=2_{testContext.TestName}";

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
        public void Test_InvokeSqlcmdDoesReturnRaisedErrorIfQueryWasRunInSingleUserMode(TestContext testContext)
        {
            var initArgs = new TestArguments
                               {
                                   Query = TestUtils.LoadSqlResource(
                                       "InvokeSqlcmdDoesNotReturnRaisedErrorIfQueryWasRunInSingleUserMode"),
                                   ConnectionString =
                                       $"{this.SqlServerInstanceInfo.ServerConnection};Application Name={testContext.TestName}"
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
        public void Test_InvokeSqlcmdReturnsErrorForArithmeticOverflowError(TestContext testContext)
        {
            var initArgs = new TestArguments
                               {
                                   Query = "SELECT convert(int,100000000000)",
                                   ConnectionString =
                                       $"{this.SqlServerInstanceInfo.ServerConnection};Application Name={testContext.TestName}"
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