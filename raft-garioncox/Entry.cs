namespace raft_garioncox;

public class Entry(int term, string value)
{
    public int Term = term;
    public string Value = value;
}