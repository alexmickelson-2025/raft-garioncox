using raft_garioncox;

public interface INode
{
    int TimeoutRate { get; set; }
    int? CurrentLeader { get; set; }
    int CommittedLogIndex { get; set; }
    int ElectionTimeout { get; set; } // in ms
    List<Entry> Entries { get; set; }
    int Id { get; set; }
    bool HasVoted { get; set; }
    Dictionary<int, INode> Neighbors { get; set; }
    NODESTATE State { get; set; }
    int Term { get; set; }
    int? Vote { get; set; }
    public Task AppendEntries(int id, int term, int committedLogIndex, Entry? entry = null);
    public void BecomeCandidate();
    public Task ReceiveAppendEntriesResponse(int followerTerm, int followerEntryIndex, bool response);
    public void ReceiveClientCommand(string command);
    public void ReceiveVote(bool vote);
    public bool RequestVoteFor(int id, int term);
    public Task RequestVoteForRPC(int cId, int cTerm);
    public void RequestVotesRPC();
    public Thread Run();
    public void Stop();
}