using raft_garioncox;

public class SimNode : INode
{
    Node node;
    public int Id { get => ((INode)node).Id; set => ((INode)node).Id = value; }
    public bool HasVoted { get => ((INode)node).HasVoted; set => ((INode)node).HasVoted = value; }
    public NODESTATE State { get => ((INode)node).State; set => ((INode)node).State = value; }
    public int Term { get => ((INode)node).Term; set => ((INode)node).Term = value; }
    public int? Vote { get => ((INode)node).Vote; set => ((INode)node).Vote = value; }
    public int ElectionTimeout { get => ((INode)node).ElectionTimeout; set => ((INode)node).ElectionTimeout = value; }
    public int? CurrentLeader { get => ((INode)node).CurrentLeader; set => ((INode)node).CurrentLeader = value; }
    public int TimeoutRate { get => ((INode)node).TimeoutRate; set => ((INode)node).TimeoutRate = value; }
    public int NetworkDelay { get; set; } = 0;
    public int CommittedLogIndex { get => ((INode)node).CommittedLogIndex; set => ((INode)node).CommittedLogIndex = value; }
    public List<Entry> Entries { get => ((INode)node).Entries; set => ((INode)node).Entries = value; }
    public Dictionary<int, INode> Neighbors { get => ((INode)node).Neighbors; set => ((INode)node).Neighbors = value; }
    public bool IsPaused { get => ((INode)node).IsPaused; set => ((INode)node).IsPaused = value; }
    public int TimeoutMultiplier { get => ((INode)node).TimeoutMultiplier; set => ((INode)node).TimeoutMultiplier = value; }

    public SimNode(Node n)
    {
        node = n;
    }

    public void BecomeCandidate()
    {
        ((INode)node).BecomeCandidate();
    }

    public void ReceiveVote(bool vote)
    {
        ((INode)node).ReceiveVote(vote);
    }

    public bool RequestVoteFor(int id, int term)
    {
        return ((INode)node).RequestVoteFor(id, term);
    }

    public Task RequestVoteForRPC(int cId, int cTerm)
    {
        Thread.Sleep(NetworkDelay);
        return ((INode)node).RequestVoteForRPC(cId, cTerm);
    }

    public void RequestVotesRPC()
    {
        ((INode)node).RequestVotesRPC();
    }

    public Thread Run()
    {
        return ((INode)node).Run();
    }

    public void Stop()
    {
        ((INode)node).Stop();
    }

    public Task AppendEntries(int id, int term, int committedLogIndex, Entry? entry = null)
    {
        return ((INode)node).AppendEntries(id, term, committedLogIndex, entry);
    }

    public Task ReceiveAppendEntriesResponse(int followerTerm, int followerEntryIndex, bool response)
    {
        return ((INode)node).ReceiveAppendEntriesResponse(followerTerm, followerEntryIndex, response);
    }

    public void ReceiveClientCommand(string command)
    {
        ((INode)node).ReceiveClientCommand(command);
    }

    public void Pause()
    {
        ((INode)node).Pause();
    }

    public void Unpause()
    {
        ((INode)node).Unpause();
    }
}