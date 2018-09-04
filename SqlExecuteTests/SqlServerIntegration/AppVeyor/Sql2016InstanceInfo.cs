namespace SqlExecuteTests.SqlServerIntegration.AppVeyor
{
    /// <inheritdoc />
    /// <summary>
    /// Instance connection info for AppVeyor SQL 2014 
    /// </summary>
    /// <seealso cref="T:SqlExecuteTests.SqlServerIntegration.SqlServerInstanceInfoBase" />
    public class Sql2016InstanceInfo : AbstractSqlServerInstanceInfo
    {
        /// <summary>
        /// The server connection
        /// </summary>
        // ReSharper disable once StyleCop.SA1309
        // ReSharper disable once InconsistentNaming
        private const string _ServerConnection = @"Server=(local)\SQL2016;User ID=sa;Password=Password12!";

        /// <summary>
        /// The instance state
        /// </summary>
        private static InstanceState state = InstanceState.Unknown;

        /// <summary>
        /// The full text installed
        /// </summary>
        private static bool fullTextInstalled;

        /// <summary>
        /// Gets a value indicating whether [full text installed].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [full text installed]; otherwise, <c>false</c>.
        /// </value>
        public override bool FullTextInstalled
        {
            get => fullTextInstalled;
            protected set => fullTextInstalled = value;
        }

        /// <summary>
        /// Gets or set the state of the instance represented by this class.
        /// </summary>
        /// <value>
        /// The state of the instance.
        /// </value>
        protected override InstanceState InstanceState
        {
            get => state;

            set => state = value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the version specific server connection.
        /// </summary>
        /// <value>
        /// The version specific server connection.
        /// </value>
        protected override string VersionSpecificServerConnection => _ServerConnection;
    }
}