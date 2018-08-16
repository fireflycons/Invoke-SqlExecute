namespace SqlExecute.Lang.Exceptions
{
    using System;

    public class UndefinedElementException : Exception
    {
        public UndefinedElementException(string msg, params string[] param) : base(String.Format(msg, param))
        {
            
        }
    }
}
