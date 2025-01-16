using raft_garioncox;

public interface INode
{
    int Id { get; }
    bool HasVoted { get; set; }
    INode[] Neighbors { get; set; }
    NODE_STATE State { get; set; }
    int Term { get; set; }
    int? Vote { get; set; }
    public bool AppendEntries(int lId, int lTerm);
    public void BecomeCandidate();
    public void Heartbeat(int lId, int lTerm);
    public bool RequestVoteFor(int cId, int cTerm);
    public Thread Run();
}