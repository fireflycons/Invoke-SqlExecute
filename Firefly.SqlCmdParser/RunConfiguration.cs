namespace Firefly.SqlCmdParser
{
    /// <summary>
    /// Run configuration
    /// </summary>
    public class RunConfiguration
    {
        /// <summary>
        /// Gets or sets the execution node number.
        /// </summary>
        /// <value>
        /// The invocation number.
        /// </value>
        public int NodeNumber { get; set; }

        /// <summary>
        /// Gets or sets the command executer.
        /// </summary>
        /// <value>
        /// The command executer.
        /// </value>
        public ICommandExecuter CommandExecuter { get; set; }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the initial batch source.
        /// </summary>
        /// <value>
        /// The initial batch source.
        /// </value>
        public IBatchSource InitialBatchSource { get; set; }

        /// <summary>
        /// Gets or sets the variable resolver.
        /// </summary>
        /// <value>
        /// The variable resolver.
        /// </value>
        public IVariableResolver VariableResolver { get; set; }

        public IOutputFileProperties OutputFile { get; set; }
    }
}