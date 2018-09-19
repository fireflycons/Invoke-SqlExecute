namespace Firefly.SqlCmdParser
{
    using System;
    using System.Data.SqlClient;
    using System.Text;

    /// <summary>
    /// Extension methods for <see cref="SqlException" />
    /// </summary>
    public static class SqlExceptionExtensions
    {
        #region Constants for indexing into the exception's Data property

        /// <summary>
        /// This element exists if AddContextData() has been called.
        /// </summary>
        public const string HasContextData = "HasContextData";

        /// <summary>
        /// Property containing SQL server instance name
        /// </summary>
        public const string Server = "Server";

        /// <summary>
        /// Property containing the start line number of the erroneous batch
        /// </summary>
        public const string BatchBeginLineNumber = "BatchBeginLineNumber";

        /// <summary>
        /// Property containing the file name of the erroneous batch
        /// </summary>
        public const string BatchSource = "BatchSource";

        /// <summary>
        /// Property indicating whether the exception was thrown from within stored procedure execution
        /// </summary>
        public const string IsProcedureError = "IsProcedureError";

        /// <summary>
        /// Property containing the line within the input file (or stored proc) where the error occurred
        /// </summary>
        public const string SourceErrorLine = "SourceErrorLine";

        /// <summary>
        /// Property containing the best guess at the statement that caused the error.
        /// </summary>
        public const string ErrorStatement = "ErrorStatement";

        #endregion

        /// <summary>
        /// Adds input batch context data to the exception data.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="batch">The batch.</param>
        /// <exception cref="ArgumentNullException">batch is null.</exception>
        public static void AddContextData(this SqlException ex, SqlBatch batch)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            var primaryException = ex.Errors[0];
            var isProcedureError = !string.IsNullOrEmpty(primaryException.Procedure);

            ex.Data.Add(Server, string.IsNullOrEmpty(ex.Server) ? "Unknown" : ex.Server);
            ex.Data.Add(BatchBeginLineNumber, batch.BatchBeginLineNumber);
            ex.Data.Add(BatchSource, batch.Source);
            ex.Data.Add(IsProcedureError, isProcedureError);
            ex.Data.Add(SourceErrorLine, primaryException.LineNumber + batch.BatchBeginLineNumber - 1);

            if (isProcedureError)
            {
                ex.Data.Add(ErrorStatement, $"EXEC {ex.Procedure} ... (in input batch)");
            }
            else
            {
                var sb = new StringBuilder();
                var lines = batch.Sql.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                for (var i = Math.Max(0, ex.LineNumber - 1);
                     i < Math.Min(lines.Length, ex.LineNumber + 10);
                     ++i)
                {
                    sb.AppendLine(lines[i]);
                }

                ex.Data.Add(ErrorStatement, sb.ToString());
            }

            // This entry just to signify that context data was successfully added.
            ex.Data.Add(HasContextData, true);
        }

        /// <summary>
        /// Gets a context data item.
        /// </summary>
        /// <typeparam name="T">Data type of the requested value</typeparam>
        /// <param name="ex">The ex.</param>
        /// <param name="itemName">Name of the item.</param>
        /// <returns>
        /// The value.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException">Exception data does not contain key {itemName}</exception>
        public static T GetContextDataItem<T>(this SqlException ex, string itemName)
        {
            if (!ex.Data.Contains(itemName))
            {
                throw new IndexOutOfRangeException($"Exception data does not contain key {itemName}");
            }

            return (T)ex.Data[itemName];
        }

        /// <summary>
        /// Formats detail of the <see cref="SqlException" /> to a string.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <returns>
        /// Formatted exception text.
        /// </returns>
        public static string Format(this SqlException ex)
        {
            var server = GetServerName(ex);
            var sb = new StringBuilder();

            sb.AppendLine("SqlException caught!")
                .AppendLine($"Client:              {Environment.MachineName}")
                .AppendLine($"Server:              {server}");

            var hasContextData = ex.Data.Contains(HasContextData);

            var batchBeginLineNumber = 0;

            if (hasContextData)
            {
                batchBeginLineNumber = ex.GetContextDataItem<int>(BatchBeginLineNumber);
                sb.AppendLine($"Batch:               {ex.GetContextDataItem<string>(BatchSource)}, beginning at line {batchBeginLineNumber}");
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

            if (hasContextData)
            {
                sb.AppendLine("Error near:");
                sb.AppendLine(ex.GetContextDataItem<string>(ErrorStatement));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Formats an <see cref="SqlError" />.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="batchBeginLineNumber">The batch begin line number.</param>
        /// <param name="indent">if set to <c>true</c> [indent].</param>
        /// <returns>
        /// Formatted error.
        /// </returns>
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

        /// <summary>
        /// Gets the name of the SQL server from the exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <returns>
        /// SQL server instance name
        /// </returns>
        private static string GetServerName(SqlException ex)
        {
            if (ex.Data.Contains(Server))
            {
                return ex.GetContextDataItem<string>(Server);
            }

            return string.IsNullOrEmpty(ex.Server) ? "Unknown" : ex.Server;
        }
    }
}