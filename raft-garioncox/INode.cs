using raft_garioncox;
using raft_garioncox.Records;

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
    public Task RequestAppendEntries(AppendEntriesDTO dto);
    public Task RespondAppendEntries(RespondEntriesDTO dto);
    public void ReceiveCommand(ClientCommandDTO dto);
    public void RespondVote(VoteResponseDTO dto);
    public Task RequestVoteRPC(VoteRequestDTO dto);
    public Thread Run();
    public void Stop();
    public void Pause();
    public void Unpause();
}