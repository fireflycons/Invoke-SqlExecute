namespace Firefly.SqlCmdParser.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <inheritdoc />
    /// <summary>
    /// Class to manage <c>:SETVAR</c> variable definition and usage.
    /// </summary>
    /// <seealso cref="T:Firefly.SqlCmdParser.IVariableResolver" />
    /// <remarks>
    /// SQLCMD scripting variable names are case insensitive.
    /// </remarks>
    public class VariableResolver : IVariableResolver
    {
        /// <summary>
        /// Variables that can only be set by the system and not via SETVAR
        /// </summary>
        private readonly IDictionary<string, string> systemReadOnlyVariables =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "SQLCMDUSER", string.Empty },
                    { "SQLCMDPASSWORD", string.Empty },
                    { "SQLCMDSERVER", "." },
                    {
                        "SQLCMDWORKSTATION",
                        Environment.MachineName
                    },
                    { "SQLCMDDBNAME", string.Empty },
                    { "SQLCMDPACKETSIZE", "4096" },
                    { "SQLCMDINI", string.Empty }
                };

        /// <summary>
        /// Variables passed in on command line if <c>overrideScriptVariables</c> is <c>true</c>
        /// </summary>
        private readonly Dictionary<string, string> initialVariables =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Variables set by SETVAR and r/w SQLCMD variables
        /// </summary>
        private readonly Dictionary<string, string> userVariables =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) 

        {
            { "SQLCMDLOGINTIMEOUT", "8" },
            { "SQLCMDSTATTIMEOUT", "0" },
            { "SQLCMDHEADERS", "0" },
            { "SQLCMDCOLSEP", " " },
            { "SQLCMDCOLWIDTH", "0" },
            { "SQLCMDERRORLEVEL", "0" },
            { "SQLCMDMAXVARTYPEWIDTH", "256" },
            { "SQLCMDMAXFIXEDTYPEWIDTH", "0" },
            { "SQLCMDEDITOR", "edit.com" },
            { "SQLCMDUSEAAD", string.Empty },

            // Extensions not defined by SQLCMD standard
            { "SQLCMDMULTISUBNETFAILOVER", "false" }
        };

        /// <summary>
        /// If true then suppress lookup of environment variables if a defined variables is not present.
        /// </summary>
        private readonly bool suppressEnvironmentVariables;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableResolver"/> class.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public VariableResolver()
        {
            this.suppressEnvironmentVariables = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableResolver" /> class.
        /// </summary>
        /// <param name="initialVariables">The initial variables passed on command line as an <see cref="IDictionary" />. Permits initialization from PowerShell hashtable.</param>
        /// <param name="overrideScriptVariables">if set to <c>true</c> then these variables will not be reset by <c>:SETVAR</c> directives within script.</param>
        /// <param name="suppressEnvironmentVariables">if set to <c>true</c> [suppress environment variables].</param>
        public VariableResolver(IDictionary initialVariables, bool overrideScriptVariables, bool suppressEnvironmentVariables = false)
        {
            this.suppressEnvironmentVariables = suppressEnvironmentVariables;
            var dict = overrideScriptVariables ? this.initialVariables : this.userVariables;

            if (initialVariables != null)
            {
                foreach (var key in initialVariables.Keys)
                {
                    dict.Add(key.ToString(), initialVariables[key].ToString());
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets all the variables currently defined within the variable resolvers
        /// </summary>
        /// <value>
        /// The variables.
        /// </value>
        public IDictionary<string, string> Variables => this.systemReadOnlyVariables
            .Concat(this.initialVariables)
            .Concat(this.userVariables)
            .OrderBy(kv => kv.Key, new VariableNameSorter())
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        /// <inheritdoc />
        /// <summary>
        /// Deletes the variable.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        public void DeleteVariable(string varName)
        {
            if (!this.initialVariables.ContainsKey(varName) && this.userVariables.ContainsKey(varName) && !varName.StartsWith("SQLCMD", StringComparison.OrdinalIgnoreCase))
            {
                // only delete user settable variables that are not SQLCMD internal
                this.userVariables.Remove(varName);
            }
        }

        /// <summary>
        /// Resolves a variable reference.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        /// <returns>
        /// The variable's value or empty string if not found.
        /// </returns>
        /// <inheritdoc />
        public string ResolveVariable(string varName)
        {
            // Check variable lists
            foreach (var varlist in new[] { this.systemReadOnlyVariables, this.initialVariables, this.userVariables })
            {
                if (varlist.ContainsKey(varName))
                {
                    return varlist[varName];
                }
            }

            // Then the environment
            if (!this.suppressEnvironmentVariables)
            {
                var envVar = Environment.GetEnvironmentVariable(varName);

                if (envVar != null)
                {
                    return envVar;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Resolves the variable ownership.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        /// <param name="varValue">The variable value.</param>
        /// <exception cref="ArgumentNullException">varName - Attempted to set a variable with null variable name</exception>
        /// <exception cref="InvalidOperationException">Cannot create variables with name beginning with SQLCMD</exception>
        /// <inheritdoc />
        public void SetVariable(string varName, string varValue)
        {
            if (varName == null)
            {
                throw new ArgumentNullException(nameof(varName), "Attempted to set a variable with null variable name");
            }

            if (this.initialVariables.ContainsKey(varName) || this.systemReadOnlyVariables.ContainsKey(varName))
            {
                // Don't overwrite value
                return;
            }

            if (this.userVariables.ContainsKey(varName))
            {
                this.userVariables[varName] = varValue;
            }
            else if (!varName.ToUpperInvariant().StartsWith("SQLCMD"))
            {
                this.userVariables.Add(varName, varValue);
            }
            else
            {
                throw new InvalidOperationException("Cannot create variables with name beginning with SQLCMD");
            }
        }

        /// <summary>
        /// Sets a system variable.
        /// </summary>
        /// <param name="varName">Name of the variable.</param>
        /// <param name="varValue">The variable value.</param>
        /// <exception cref="ArgumentNullException">varName - Attempted to set a system variable with null variable name</exception>
        public void SetSystemVariable(string varName, string varValue)
        {
            if (varName == null)
            {
                throw new ArgumentNullException(nameof(varName), "Attempted to set a system variable with null variable name");
            }

            if (!varName.ToUpperInvariant().StartsWith("SQLCMD"))
            {
                return;
            }

            foreach (var varlist in new[] { this.systemReadOnlyVariables, this.userVariables })
            {
                if (varlist.ContainsKey(varName))
                {
                    varlist[varName] = varValue;
                    return;
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Key comparer for variable list. Sorts SQLCMD variables above user variables
        /// </summary>
        /// <seealso cref="T:System.String" />
        private class VariableNameSorter : IComparer<string>
        {
            /// <inheritdoc />
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />.Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than <paramref name="y" />.
            /// </returns>
            public int Compare(string x, string y)
            {
                // ReSharper disable PossibleNullReferenceException - Dictionary keys cannot be null
                var x1 = x.ToUpperInvariant();
                var y1 = y.ToUpperInvariant();

                // ReSharper restore PossibleNullReferenceException
                if (x1.StartsWith("SQLCMD") && y1.StartsWith("SQLCMD"))
                {
                    return string.Compare(x1, y1, StringComparison.OrdinalIgnoreCase);
                }

                if (x1.StartsWith("SQLCMD"))
                {
                    // SQLCMD variables are first in order
                    return -1;
                }

                return y1.StartsWith("SQLCMD") ? 1 : string.Compare(x1, y1, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}