using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Firefly.SqlCmdParser
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class InputSourceChangedEventArgs : EventArgs
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Firefly.SqlCmdParser.InputSourceChangedEventArgs" /> class.
        /// </summary>
        /// <param name="newSource">The new source.</param>
        internal InputSourceChangedEventArgs(IBatchSource newSource)
        {
            this.Source = newSource;
        }

        /// <summary>
        /// Gets the new source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public IBatchSource Source { get; }
    }
}
