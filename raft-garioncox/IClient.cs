public interface IClient
{
    int Id { get; }
    Dictionary<int, INode> Neighbors { get; set; }
    public Task ReceiveLeaderCommitResponse(string command, bool status);
}