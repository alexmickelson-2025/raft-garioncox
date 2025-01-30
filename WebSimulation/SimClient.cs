

public class SimClient(int id) : IClient
{
    public int Id { get; private set; } = id;
    public Dictionary<int, INode> Neighbors { get; set; } = [];

    public Task ReceiveLeaderCommitResponse(string command, bool status)
    {
        return Task.CompletedTask;
        // throw new NotImplementedException();
    }
}