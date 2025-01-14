using System.Data;

namespace raft_garioncox;

public class Node(int id) : INode
{
    public NODE_STATE State { get; set; } = NODE_STATE.FOLLOWER;
    public bool HasVoted { get; private set; } = false;
    public int Id { get; } = id;
    public int Term { get; set; } = 0;
    public INode? Vote { get; private set; } = null;
    public INode[] Neighbors { get; set; } = [];

    public void SetCandidate()
    {
        State = NODE_STATE.CANDIDATE;
        Term += 1;
        Vote = this;
        HasVoted = true;

        GetVotes();
    }

    private void GetVotes()
    {
        int tally = 1;
        int required = (int)Math.Ceiling((double)Neighbors.Length / 2);
        foreach (INode node in Neighbors)
        {
            tally += node.RequestToVoteFor(this) ? 1 : 0;
        }

        if (tally >= required)
        {
            State = NODE_STATE.LEADER;
        }
    }

    public bool RequestToVoteFor(INode n)
    {
        if (HasVoted) { return false; }

        HasVoted = true;
        Vote = n;
        return true;
    }

    public void AppendEntries(Node n)
    {
        if (n.Term >= Term)
        {
            State = NODE_STATE.FOLLOWER;
        }
    }
}
