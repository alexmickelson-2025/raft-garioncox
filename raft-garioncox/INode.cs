using raft_garioncox;

public interface INode
{
    public bool RequestToVoteFor(INode n);
    public void Heartbeat(INode n);
    public void SetCandidate();
    public bool AppendEntries(INode n);
    public void Run();
    NODE_STATE State { get; set; }
    bool HasVoted { get; set; }
    int Id { get; }
    int Term { get; set; }
    INode? Vote { get; set; }
    INode[] Neighbors { get; set; }
}