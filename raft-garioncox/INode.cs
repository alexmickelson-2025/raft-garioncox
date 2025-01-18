using raft_garioncox;

public interface INode
{
    int TimeoutRate { get; set; }
    int? CurrentLeader { get; set; }
    int ElectionTimeout { get; set; } // in ms
    int Id { get; set; }
    bool HasVoted { get; set; }
    INode[] Neighbors { get; set; }
    NODESTATE State { get; set; }
    int Term { get; set; }
    int? Vote { get; set; }
    public bool AppendEntries(int id, int term);
    public void BecomeCandidate();
    public void ReceiveVote(bool vote);
    public bool RequestVoteFor(int id, int term);
    public Task RequestVoteForRPC(int cId, int cTerm);
    public void RequestVotesRPC();
    public Thread Run();
    public void Stop();
}