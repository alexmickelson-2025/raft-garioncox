using raft_garioncox;

public class SimNode : INode
{
    Node node;

    public NODE_STATE State { get => ((INode)node).State; set => ((INode)node).State = value; }
    public bool HasVoted { get => ((INode)node).HasVoted; set => ((INode)node).HasVoted = value; }

    public int Id => ((INode)node).Id;

    public int Term { get => ((INode)node).Term; set => ((INode)node).Term = value; }
    public INode? Vote { get => ((INode)node).Vote; set => ((INode)node).Vote = value; }
    public INode[] Neighbors { get => ((INode)node).Neighbors; set => ((INode)node).Neighbors = value; }

    public SimNode(Node n)
    {
        node = n;
    }

    public void Heartbeat(INode n)
    {
        ((INode)node).Heartbeat(n);
    }

    public bool RequestToVoteFor(INode n)
    {
        return ((INode)node).RequestToVoteFor(n);
    }

    public void SetCandidate()
    {
        ((INode)node).SetCandidate();
    }

    public bool AppendEntries(INode n)
    {
        return ((INode)node).AppendEntries(n);
    }

    public void Run()
    {
        ((INode)node).Run();
    }
}