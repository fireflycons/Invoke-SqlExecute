namespace SqlExecute.Lang.Lexers
{
    using System;
    using System.Collections.Generic;

    public class TokenizableStreamBase<T> where T : class
    {
        public TokenizableStreamBase(Func<List<T>> extractor)
        {
            this.Index = 0;

            this.Items = extractor();

            this.SnapshotIndexes = new Stack<int>();
        }

        private List<T> Items { get; set; }

        protected int Index { get; set; }

        private Stack<int> SnapshotIndexes { get; set; }

        public virtual T Current
        {
            get
            {
                if (this.EOF(0))
                {
                    return null;
                }

                return this.Items[this.Index];
            }
        }

        public void Consume()
        {
            this.Index++;
        }

        private Boolean EOF(int lookahead)
        {
            if (this.Index + lookahead >= this.Items.Count)
            {
                return true;
            }

            return false;
        }

        public Boolean End()
        {
            return this.EOF(0);
        }

        public virtual T Peek(int lookahead)
        {
            if (this.EOF(lookahead))
            {
                return null;
            }

            return this.Items[this.Index + lookahead];
        }

        public void TakeSnapshot()
        {
            this.SnapshotIndexes.Push(this.Index);
        }

        public void RollbackSnapshot()
        {
            this.Index = this.SnapshotIndexes.Pop();
        }

        public void CommitSnapshot()
        {
            this.SnapshotIndexes.Pop();
        }
    }
}
