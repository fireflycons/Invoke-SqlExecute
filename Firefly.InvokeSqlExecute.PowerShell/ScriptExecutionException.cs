﻿// ReSharper disable InheritdocConsiderUsage
namespace Firefly.InvokeSqlExecute
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Runtime.Serialization;

    /// <summary>
    /// If the execution is set to continue on error, this exception is thrown at the end
    /// to indicate the total number of errors that were detected.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class ScriptExecutionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class.
        /// </summary>
        /// <param name="errorCount">The error count.</param>
        public ScriptExecutionException(int errorCount)
        {
            this.SqlExceptions = new List<SqlException>();
        }

        public ScriptExecutionException(IList<SqlException> sqlExceptions)
        {
            this.SqlExceptions = sqlExceptions;
        }

        public ScriptExecutionException(SqlException sqlException)
        {
            this.SqlExceptions = new List<SqlException> { sqlException };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public ScriptExecutionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected ScriptExecutionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the error count.
        /// </summary>
        /// <value>
        /// The error count.
        /// </value>
        public int ErrorCount => this.SqlExceptions.Count;

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message => $"{this.ErrorCount} error(s) were detected. Please see log for details.";

        public IList<SqlException> SqlExceptions { get; }
    }
}