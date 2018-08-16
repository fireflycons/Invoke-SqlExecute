using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firefly.InvokeSqlExecute
{
    using System.Management.Automation;

    /// <summary>
    /// Extension methods for <see cref="PSObject"/>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class PSObjectExtensions
    {
        /// <summary>
        /// Determines whether the type wrapped by the <see cref="PSObject"/> is an instance of the specified type.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="typeName">The type name.</param>
        /// <returns>
        ///   <c>true</c> if the type wrapped by the <see cref="PSObject"/> is an instance of the specified type; otherwise, <c>false</c>.
        /// </returns>
        public static bool Is(this PSObject obj, string typeName)
        {
            return obj.TypeNames.Contains(typeName);
        }
    }
}
