using raft_garioncox;
using raft_garioncox.Records;

public class SimNode : INode
{
    Node node;
    public int Id { get => ((INode)node).Id; }
    public NODESTATE State { get => node.State; set => node.State = value; }
    public int Term { get => node.Term; set => node.Term = value; }
    public int NetworkDelay { get; set; } = 0;
    public int CommittedLogIndex { get => node.CommittedLogIndex; set => node.CommittedLogIndex = value; }
    public List<Entry> Entries { get => node.Entries; set => node.Entries = value; }
    public int? CurrentLeader { get => node.CurrentLeader; set => node.CurrentLeader = value; }
    public int ElectionTimeout { get => node.ElectionTimeout; set => node.ElectionTimeout = value; }
    public bool IsPaused { get => node.IsPaused; set => node.IsPaused = value; }
    public Dictionary<int, INode> Neighbors { get => node.Neighbors; set => node.Neighbors = value; }
    public int IntervalScalar { get => node.IntervalScalar; set => node.IntervalScalar = value; }
    public string LogState { get => node.LogState; }

    public SimNode(Node n)
    {
        node = n;
    }

    public Task RespondVote(VoteResponseDTO dto)
    {
        ((INode)node).RespondVote(dto);
        return Task.CompletedTask;
    }

    public Task RequestVoteRPC(VoteRequestDTO dto)
    {
        Thread.Sleep(NetworkDelay);
        return ((INode)node).RequestVoteRPC(dto);
    }

    public Thread Run()
    {
        return node.Run();
    }

    public void Stop()
    {
        node.Stop();
    }

    public void Pause()
    {
        node.Pause();
    }

    public void Unpause()
    {
        node.Unpause();
    }

    public Task RespondAppendEntries(RespondEntriesDTO dto)
    {
        return ((INode)node).RespondAppendEntries(dto);
    }

    public Task RequestAppendEntries(AppendEntriesDTO dto)
    {
        return ((INode)node).RequestAppendEntries(dto);
    }

    public Task ReceiveCommand(ClientCommandDTO dto)
    {
        ((INode)node).ReceiveCommand(dto);
        return Task.CompletedTask;
    }

    public Task ResetElectionTimeout(bool isLeader = false)
    {
        node.ResetElectionTimeout(isLeader);
        return Task.CompletedTask;
    }
}