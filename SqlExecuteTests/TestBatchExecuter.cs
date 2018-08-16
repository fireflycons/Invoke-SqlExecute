using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlExecuteTests
{
    using System.Diagnostics;

    using Firefly.InvokeSqlExecute;
    using Firefly.SqlCmdParser;
    using Firefly.SqlCmdParser.Client;

    public class TestBatchExecuter : CommandExecuter
    {
        /// <summary>
        /// Called when the parser has a complete batch to process.
        /// </summary>
        /// <param name="batch">The batch to process.</param>
        /// <param name="numberOfExecutions">The number of times to execute the batch (e.g. <c>GO 2</c> to execute the batch twice.</param>
        /// <inheritdoc />
        public override void ProcessBatch(SqlBatch batch, int numberOfExecutions)
        {
            var sql = batch.Sql;

            Debug.WriteLine($"#### '{batch.Source}', Line {batch.BatchBeginLineNumber}, Execution Count: {numberOfExecutions} ####");

            if (string.IsNullOrWhiteSpace(sql))
            {
                Debug.WriteLine("Empty batch ignored");
            }
            else
            {
                Debug.WriteLine(sql);
            }

        }

        public TestBatchExecuter(ISqlExecuteArguments args, VariableResolver variableResolver)
            : base(args, variableResolver)
        {
        }
    }
}
