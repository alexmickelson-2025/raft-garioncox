using System.Data;

namespace raft_garioncox;

public class Node(int id) : INode
{
    public NODE_STATE State { get; private set; } = NODE_STATE.FOLLOWER;
    public bool HasVoted { get; private set; } = false;
    public int Id { get; } = id;
    public int Term { get; private set; } = 0;
    public int? Vote { get; private set; } = null;
    public INode[] Neighbors { get; set; } = [];

    public void SetCandidate()
    {
        State = NODE_STATE.CANDIDATE;
        Term += 1;
        Vote = Id;
        HasVoted = true;

        foreach (INode node in Neighbors)
        {
            node.RequestVote(this);
        }

        State = NODE_STATE.LEADER;
    }

    public void RequestVote(INode n)
    {
        throw new NotImplementedException();
    }
}
