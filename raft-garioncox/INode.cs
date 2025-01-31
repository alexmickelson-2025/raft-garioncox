using raft_garioncox;
using raft_garioncox.Records;

public interface INode
{
    int CommittedLogIndex { get; }
    List<Entry> Entries { get; set; }
    int Id { get; }
    Dictionary<int, INode> Neighbors { get; set; }
    NODESTATE State { get; }
    int Term { get; }
    public Task RequestAppendEntries(AppendEntriesDTO dto);
    public Task RespondAppendEntries(RespondEntriesDTO dto);
    public Task ReceiveCommand(ClientCommandDTO dto);
    public Task RespondVote(VoteResponseDTO dto);
    public Task RequestVoteRPC(VoteRequestDTO dto);
}