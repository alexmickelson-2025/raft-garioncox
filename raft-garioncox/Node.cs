using System.Data;

namespace raft_garioncox;

public class Node(int id)
{
    public NODE_STATE State { get; private set; } = NODE_STATE.FOLLOWER;
    public bool HasVoted { get; private set; } = false;
    public int Id { get; } = id;
    public int Term { get; private set; } = 0;
    public int? Vote { get; private set; } = null;

    public void SetCandidate()
    {
        State = NODE_STATE.CANDIDATE;
        Term += 1;
        Vote = Id;
        HasVoted = true;
    }
}
