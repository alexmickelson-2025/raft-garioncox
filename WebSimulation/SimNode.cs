using raft_garioncox;

public class SimNode : INode
{
    Node node;
    public int Id { get => ((INode)node).Id; set => ((INode)node).Id = value; }
    public bool HasVoted { get => ((INode)node).HasVoted; set => ((INode)node).HasVoted = value; }
    public INode[] Neighbors { get => ((INode)node).Neighbors; set => ((INode)node).Neighbors = value; }
    public NODESTATE State { get => ((INode)node).State; set => ((INode)node).State = value; }
    public int Term { get => ((INode)node).Term; set => ((INode)node).Term = value; }
    public int? Vote { get => ((INode)node).Vote; set => ((INode)node).Vote = value; }
    public int ElectionTimeout { get => ((INode)node).ElectionTimeout; set => ((INode)node).ElectionTimeout = value; }
    public int? CurrentLeader { get => ((INode)node).CurrentLeader; set => ((INode)node).CurrentLeader = value; }
    public int TimeoutRate { get => ((INode)node).TimeoutRate; set => ((INode)node).TimeoutRate = value; }

    public SimNode(Node n)
    {
        node = n;
    }

    public bool AppendEntries(int id, int term)
    {
        return ((INode)node).AppendEntries(id, term);
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
}