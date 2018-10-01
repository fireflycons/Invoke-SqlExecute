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
        public ISqlServerInstanceInfo SqlServerInstanceInfo { get; } = new T();

        /// <summary>
        /// Tests the invoke SQLCMD does not return sp name nor line when error occurs in procedure.
        /// </summary>
        public void Should_report_stored_procedure_details_in_error_raised_within_an_executing_procedure(TestContext testContext)
        {
            var initArgs = new TestArguments
                               {
                                   Query =
                                       TestUtils.LoadSqlResource(
                                           "InvokeSqlcmdDoesNotReturnSpNameNorLineWhenErrorOccursInProcedure"),
                                   ConnectionString =
                                       new[]
                                           {
                                               $"{this.SqlServerInstanceInfo.GetServerConnection()};Database={TestUtils.DatabaseName};Application Name=1_{testContext.TestName}"
                                           }
                               };

            using (var impl = new SqlExecuteImpl(initArgs))
            {
                // Create error proc
                impl.Execute();
            }

            // ReSharper disable once StringLiteralTypo
            initArgs.Query = "EXEC dbo.geterror";
            initArgs.ConnectionString =
                new[]
                    {$"{this.SqlServerInstanceInfo.GetServerConnection()};Database={TestUtils.DatabaseName};Application Name=2_{testContext.TestName}"};

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
                        // ReSharper disable once StringLiteralTypo
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
        public void Should_correctly_RAISERROR_when_database_set_to_single_user_mode(TestContext testContext)
        {
            var initArgs = new TestArguments
                               {
                                   Query = TestUtils.LoadSqlResource(
                                       "InvokeSqlcmdDoesNotReturnRaisedErrorIfQueryWasRunInSingleUserMode"),
                                   ConnectionString =
                                       new[]
                                           { $"{this.SqlServerInstanceInfo.GetServerConnection()};Application Name={testContext.TestName}"}
                               };

            using (var impl = new SqlExecuteImpl(initArgs))
            {
                try
                {
                    impl.Execute();
                }
                catch (SqlException e)
                {
                    Assert.AreEqual("First Error.", e.Message);
                }
            }
        }

        /// <summary>
        /// Tests the invoke SQLCMD returns error for arithmetic overflow error.
        /// </summary>
        public void Should_RAISERROR_on_arithmetic_overflow(TestContext testContext)
        {
            var initArgs = new TestArguments
                               {
                                   Query = "SELECT convert(int,100000000000)",
                                   ConnectionString =
                                       new[]
                                           { $"{this.SqlServerInstanceInfo.GetServerConnection()};Application Name={testContext.TestName}"}
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