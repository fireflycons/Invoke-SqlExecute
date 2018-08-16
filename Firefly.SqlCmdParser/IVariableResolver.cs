﻿namespace Firefly.SqlCmdParser
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface that defines how to manage SQLCMD variables
    /// Implementations should maintain a list of variables as they are set 
    /// and also service environment variables as per SQLCMD documentation.
    /// </summary>
    public interface IVariableResolver
    {
        /// <summary>
        /// Gets all the variables currently defined within the variable resolver.
        /// </summary>
        /// <value>
        /// The variables.
        /// </value>
        IDictionary<string, string> Variables { get; }

        /// <summary>
        /// Deletes the variable.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        void DeleteVariable(string varName);

        /// <summary>
        /// Resolves the variable.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        /// <returns>The variable's value or empty string if not found.</returns>
        string ResolveVariable(string varName);

        /// <summary>
        /// Resolves the variable ownership.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        /// <param name="varValue">The variable value.</param>
        /// <returns><c>true</c> if ownership of the variable is assumed by the implementation; else <c>false</c></returns>
        void SetVariable(string varName, string varValue);
    }
}