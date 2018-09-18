namespace Firefly.SqlCmdParser
{
    using System.Data;

    /// <summary>
    /// Defines how the results of batch executions should be returned
    /// </summary>
    public enum OutputAs
    {
        /// <summary>
        /// Ignore results
        /// </summary>
        None,

        /// <summary>
        /// Return scalar result
        /// </summary>
        Scalar,

        /// <summary>
        /// Return list of <see cref="DataRow"/> 
        /// </summary>
        DataRows,

        /// <summary>
        /// Return a <see cref="DataSet"/> 
        /// </summary>
        DataSet,

        /// <summary>
        /// Return one or more <see cref="DataTable"/>
        /// </summary>
        DataTables
    }
}