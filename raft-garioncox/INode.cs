using raft_garioncox;
using raft_garioncox.Records;

public interface INode
{
    int Id { get; }
    public Task RequestAppendEntries(AppendEntriesDTO dto);
    public Task RespondAppendEntries(RespondEntriesDTO dto);
    public Task ReceiveCommand(ClientCommandDTO dto);
    public Task RespondVote(VoteResponseDTO dto);
    public Task RequestVoteRPC(VoteRequestDTO dto);
}