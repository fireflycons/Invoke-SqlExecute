namespace SqlExecute.Lang.Data
{
    class ValueMemory
    {
        public dynamic Value { get; set; }
        public MemorySpace Memory { get; set; }

        public ValueMemory(dynamic value, MemorySpace memory)
        {
            this.Value = value;
            this.Memory = memory;
        }
    }
}
