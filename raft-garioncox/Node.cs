namespace raft_garioncox;

public class Node(int id) : INode
{
    public NODE_STATE State { get; set; } = NODE_STATE.FOLLOWER;
    public bool HasVoted { get; set; } = false;
    public int Id { get; } = id;
    public int Term { get; set; } = 0;
    public INode? Vote { get; set; } = null;
    public INode[] Neighbors { get; set; } = [];
    public INode? CurrentLeader { get; set; } = null;
    public volatile int ElectionTimeout = 300; // in ms
    public bool Running = true;

    public void BecomeCandidate()
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
        int required = (int)Math.Ceiling((Neighbors.Length + 1.0) / 2);
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
        if (HasVoted && n.Term <= Term) { return false; }

        HasVoted = true;
        Vote = n;
        return true;
    }

    public bool AppendEntries(INode n)
    {
        if (n.Term >= Term)
        {
            State = NODE_STATE.FOLLOWER;
            CurrentLeader = n;
            return true;
        }

        return false;
    }

    public void Heartbeat(INode n)
    {
        throw new NotImplementedException();
    }

    public void Run()
    {
        while (Running)
        {
            if (State == NODE_STATE.LEADER)
            {
                foreach (INode n in Neighbors)
                {
                    n.Heartbeat(this);
                }

                try
                {
                    Thread.Sleep(50);
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }

            if (State == NODE_STATE.FOLLOWER)
            {
                ElectionTimeout -= 10;

                if (ElectionTimeout <= 0)
                {
                    BecomeCandidate();
                }

                try
                {
                    Thread.Sleep(10);
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        }
    }

    public void Stop()
    {
        Running = false;
    }
}
