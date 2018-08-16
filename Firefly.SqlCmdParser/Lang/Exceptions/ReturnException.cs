namespace SqlExecute.Lang.Exceptions
{
    using System;

    public class ReturnException : Exception
    {
        public dynamic Value { get; private set; }

        public ReturnException(dynamic value)
        {
            this.Value = value;
        }
    }
}
