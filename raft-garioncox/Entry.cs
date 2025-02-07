namespace raft_garioncox;

public class Entry
{
    public int Term { get; private set; }
    public string Value { get; private set; } = "";

    public Entry(int term, string value)
    {
        Term = term;
        Value = value;
    }
}