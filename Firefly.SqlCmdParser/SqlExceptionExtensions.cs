namespace Firefly.SqlCmdParser
{
    using System;
    using System.Data.SqlClient;
    using System.Text;

    /// <summary>
    /// Extension methods for <see cref="SqlException"/>
    /// </summary>
    public static class SqlExceptionExtensions
    {
        /// <summary>
        /// Formats detail of the <see cref="SqlException"/> to a string.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="batch">The batch.</param>
        /// <returns>Formatted string</returns>
        public static string Format(this SqlException ex, SqlBatch batch)
        {
            return ex.Format(batch, string.IsNullOrEmpty(ex.Server) ? "Unknown" : ex.Server);
        }

        /// <summary>
        /// Formats detail of the <see cref="SqlException"/> to a string.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="batch">The batch.</param>
        /// <param name="server">The server.</param>
        /// <returns>Formatted exception text.</returns>
        public static string Format(this SqlException ex, SqlBatch batch, string server)
        {
            // If a SQLBatch is included in the exception user data, use it.
            if (batch == null && ex.Data.Contains("Batch"))
            {
                batch = (SqlBatch)ex.Data["Batch"];
            }

            var sb = new StringBuilder();

            sb.AppendLine("SqlException caught!")
                .AppendLine($"Client:              {Environment.MachineName}")
                .AppendLine($"Server:              {server}");

            var batchBeginLineNumber = 0;

            if (batch != null)
            {
                batchBeginLineNumber = batch.BatchBeginLineNumber;
                sb.AppendLine($"Batch:               {batch.Source}, beginning at line {batchBeginLineNumber}");
            }

            sb.AppendLine(FormatSqlError(ex.Errors[0], batchBeginLineNumber, false));

            if (ex.Errors.Count > 1)
            {
                sb.AppendLine("Errors:");

                for (var i = 1; i < ex.Errors.Count; ++i)
                {
                    sb.AppendLine($"Error #{i}").AppendLine(FormatSqlError(ex.Errors[i], batchBeginLineNumber, true));
                }
            }

            if (batch != null && string.IsNullOrEmpty(ex.Procedure))
            {
                // Can only pinpoint the error line if the error is in the batch itself and not within a called procedure.
                sb.AppendLine("Error near:");
                var lines = batch.Sql.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                for (var i = Math.Max(0, ex.LineNumber - 1);
                     i < Math.Min(lines.Length, ex.LineNumber + 10);
                     ++i)
                {
                    sb.AppendLine(lines[i]);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Formats an <see cref="SqlError"/>.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="batchBeginLineNumber">The batch begin line number.</param>
        /// <param name="indent">if set to <c>true</c> [indent].</param>
        /// <returns>Formatted error.</returns>
        private static string FormatSqlError(SqlError error, int batchBeginLineNumber, bool indent)
        {
            string pad = indent ? "  " : string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine($"{pad}Message:             {error.Message}")
                .AppendLine($"{pad}Source:              {error.Source}")
                .AppendLine($"{pad}Number:              {error.Number}")
                .AppendLine($"{pad}Class:               {error.Class}")
                .AppendLine($"{pad}State:               {error.State}");

            if (!string.IsNullOrEmpty(error.Procedure))
            {
                // Line number in the exception is the line within the procedure that had the error, 
                // not the line on which the procedure occurs in the batch
                sb.AppendLine($"{pad}Procedure:           {error.Procedure}")
                  .AppendLine($"{pad}Line (within proc):  {error.LineNumber}");
            }
            else if (batchBeginLineNumber > 0)
            {
                sb.AppendLine($"{pad}Line (within batch): {error.LineNumber}")
                    .AppendLine($"{pad}Line (within file):  {error.LineNumber + batchBeginLineNumber - 1}");
            }

            return sb.ToString();
        }
    }
}