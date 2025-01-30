using raft_garioncox;

public interface INode
{
    int? CurrentLeader { get; set; }
    int CommittedLogIndex { get; set; }
    int ElectionTimeout { get; set; } // in ms
    List<Entry> Entries { get; set; }
    int Id { get; set; }
    public int IntervalScalar { get; set; }
    public bool IsPaused { get; set; }
    Dictionary<int, INode> Neighbors { get; set; }
    NODESTATE State { get; set; }
    int Term { get; set; }
    public Task RequestAppendEntries(int leaderId, int leaderTerm, int committedLogIndex, int previousEntryIndex, int previousEntryTerm, List<Entry> entries);
    public Task RespondAppendEntries(int followerId, int followerTerm, int followerEntryIndex, bool response);
    public void ReceiveCommand(IClient client, string command);
    public void RespondVote(bool vote);
    public Task RequestVoteRPC(int cId, int cTerm);
    public Thread Run();
    public void Stop();
    public void Pause();
    public void Unpause();
}