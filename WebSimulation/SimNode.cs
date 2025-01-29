using raft_garioncox;

public class SimNode : INode
{
    Node node;
    public int Id { get => ((INode)node).Id; set => ((INode)node).Id = value; }
    public NODESTATE State { get => ((INode)node).State; set => ((INode)node).State = value; }
    public int Term { get => ((INode)node).Term; set => ((INode)node).Term = value; }
    public int IntervalScalar { get => INode.IntervalScalar; set => INode.IntervalScalar = value; }
    public int NetworkDelay { get; set; } = 0;
    public int CommittedLogIndex { get => ((INode)node).CommittedLogIndex; set => ((INode)node).CommittedLogIndex = value; }
    public List<Entry> Entries { get => ((INode)node).Entries; set => ((INode)node).Entries = value; }
    public int? CurrentLeader { get => ((INode)node).CurrentLeader; set => ((INode)node).CurrentLeader = value; }
    public int ElectionTimeout { get => ((INode)node).ElectionTimeout; set => ((INode)node).ElectionTimeout = value; }
    public bool IsPaused { get => ((INode)node).IsPaused; set => ((INode)node).IsPaused = value; }
    public Dictionary<int, INode> Neighbors { get => ((INode)node).Neighbors; set => ((INode)node).Neighbors = value; }

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
}