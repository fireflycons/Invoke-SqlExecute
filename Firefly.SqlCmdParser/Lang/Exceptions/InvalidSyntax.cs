namespace SqlExecute.Lang.Exceptions
{
    using System;

    public class InvalidSyntax : Exception
    {
        public InvalidSyntax(string format) : base(format)
        {
        }
    }
}
