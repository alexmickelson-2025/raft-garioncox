using raft_garioncox;

public interface INode
{
    public bool RequestVoteFor(int cId, int cTerm);
    public void Heartbeat(int lId, int lTerm);
    public void BecomeCandidate();
    public bool AppendEntries(int lId, int lTerm);
    public Thread Run();
    NODE_STATE State { get; set; }
    bool HasVoted { get; set; }
    int Id { get; }
    int Term { get; set; }
    int? Vote { get; set; }
    INode[] Neighbors { get; set; }
}