namespace SqlExecuteTests
{
    using System.Diagnostics;

    using Firefly.SqlCmdParser;
    using Firefly.SqlCmdParser.Client;

    /// <summary>
    /// Sub-class of <see cref="CommandExecuter"/> for testing
    /// </summary>
    /// <seealso cref="Firefly.SqlCmdParser.Client.CommandExecuter" />
    public class TestBatchExecuter : CommandExecuter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestBatchExecuter"/> class.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="variableResolver">The variable resolver.</param>
        public TestBatchExecuter(ISqlExecuteArguments args, VariableResolver variableResolver)
            : base(0, args, variableResolver)
        {
        }

        /// <summary>
        /// Called when the parser has a complete batch to process.
        /// </summary>
        /// <param name="batch">The batch to process.</param>
        /// <param name="numberOfExecutions">The number of times to execute the batch (e.g. <c>GO 2</c> to execute the batch twice.</param>
        /// <remarks>
        /// If the current error mode (as set by <c>:ON ERROR</c>) is IGNORE, then any <see cref="T:System.Data.SqlClient.SqlException" /> should be caught and
        /// sent to the STDERR channel, else it should be thrown and the client should handle it.
        /// </remarks>
        /// <inheritdoc />
        public override void ProcessBatch(SqlBatch batch, int numberOfExecutions)
        {
            var sql = batch.Sql;

            Debug.WriteLine(
                $"#### '{batch.Source}', Line {batch.BatchBeginLineNumber}, Execution Count: {numberOfExecutions} ####");

            Debug.WriteLine(string.IsNullOrWhiteSpace(sql) ? "Empty batch ignored" : sql);
        }
    }
}