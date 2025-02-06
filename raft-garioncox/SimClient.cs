namespace raft_garioncox;

public class SimClient(int id) : IClient
{
    public int Id { get; private set; } = id;
    public Dictionary<int, INode> Neighbors { get; set; } = [];
    public Dictionary<string, bool?> CommittedRequests = [];

    public Task ReceiveLeaderCommitResponse(string command, bool status)
    {
        if (status)
        {
            CommittedRequests[command] = true;
        }

        return Task.CompletedTask;
    }

    public void AddCommand(string command)
    {
        CommittedRequests[command] = false;
    }
}