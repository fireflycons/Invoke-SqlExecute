using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firefly.SqlCmdParser
{
    using System.Runtime.Serialization;

    using Firefly.SqlCmdParser.SimpleParser;

    [Serializable]
    public class UnclosedStringLiteralException : ParserException
    {
        internal UnclosedStringLiteralException(TokenizerState tokenizerState, IBatchSource batchSource)
            : base(tokenizerState, batchSource)
        {
        }

        protected UnclosedStringLiteralException()
        {
        }

        protected UnclosedStringLiteralException(string message)
            : base(message)
        {
        }

        protected UnclosedStringLiteralException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected UnclosedStringLiteralException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
