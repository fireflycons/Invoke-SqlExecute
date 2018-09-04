using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlExecuteTests.SqlServerIntegration
{
    /// <summary>
    /// 
    /// </summary>
    public enum InstanceState
    {
        /// <summary>
        /// Instance availability as yet undetermined
        /// </summary>
        Unknown,

        /// <summary>
        /// Instance is available
        /// </summary>
        Available,

        /// <summary>
        /// Instance is unavailable
        /// </summary>
        Unavailable
    }
}
