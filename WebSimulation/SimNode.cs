using raft_garioncox;

public class SimNode : INode
{
    Node node;
    public int Id { get => ((INode)node).Id; set => ((INode)node).Id = value; }
    public NODESTATE State { get => node.State; set => node.State = value; }
    public int Term { get => node.Term; set => node.Term = value; }
    public int NetworkDelay { get; set; } = 0;
    public int CommittedLogIndex { get => node.CommittedLogIndex; set => node.CommittedLogIndex = value; }
    public List<Entry> Entries { get => node.Entries; set => node.Entries = value; }
    public int? CurrentLeader { get => node.CurrentLeader; set => node.CurrentLeader = value; }
    public int ElectionTimeout { get => node.ElectionTimeout; set => node.ElectionTimeout = value; }
    public bool IsPaused { get => node.IsPaused; set => node.IsPaused = value; }
    public Dictionary<int, INode> Neighbors { get => node.Neighbors; set => node.Neighbors = value; }
    public int IntervalScalar { get => ((INode)node).IntervalScalar; set => ((INode)node).IntervalScalar = value; }

    public SimNode(Node n)
    {
        node = n;
    }

    public void RespondVote(bool vote)
    {
        ((INode)node).RespondVote(vote);
    }

    public bool RequestVote(int id, int term)
    {
        return ((INode)node).RequestVote(id, term);
    }

    public Task RequestVoteForRPC(int cId, int cTerm)
    {
        Thread.Sleep(NetworkDelay);
        return ((INode)node).RequestVoteForRPC(cId, cTerm);
    }

    public Thread Run()
    {
        return ((INode)node).Run();
    }

    public void Stop()
    {
        ((INode)node).Stop();
    }

    public void Pause()
    {
        ((INode)node).Pause();
    }

    public void Unpause()
    {
        ((INode)node).Unpause();
    }

    public Task RespondAppendEntries(int followerId, int followerTerm, int followerEntryIndex, bool response)
    {
        return ((INode)node).RespondAppendEntries(followerId, followerTerm, followerEntryIndex, response);
    }

    public Task RequestAppendEntries(int leaderId, int leaderTerm, int committedLogIndex, int previousEntryIndex, int previousEntryTerm, List<Entry> entries)
    {
        return ((INode)node).RequestAppendEntries(leaderId, leaderTerm, committedLogIndex, previousEntryIndex, previousEntryTerm, entries);
    }

    public void ReceiveCommand(IClient client, string command)
    {
        ((INode)node).ReceiveCommand(client, command);
    }

    public void ResetElectionTimeout(bool isLeader = false)
    {
        node.ResetElectionTimeout(isLeader);
    }
}