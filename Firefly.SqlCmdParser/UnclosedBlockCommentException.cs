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
    public class UnclosedBlockCommentException : ParserException
    {
        internal UnclosedBlockCommentException(TokenizerState tokenizerState, IBatchSource batchSource)
            : base(tokenizerState, batchSource)
        {
        }

        protected UnclosedBlockCommentException()
        {
        }

        protected UnclosedBlockCommentException(string message)
            : base(message)
        {
        }

        protected UnclosedBlockCommentException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected UnclosedBlockCommentException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
