namespace SqlExecute.Lang.Matches
{
    using SqlExecute.Lang.Data;
    using SqlExecute.Lang.Lexers;

    public interface IMatcher 
    {
        Token IsMatch(Tokenizer tokenizer);
    }
}
