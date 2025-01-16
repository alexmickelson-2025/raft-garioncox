namespace raft_garioncox;

public class Node : INode
{
    public NODE_STATE State { get; set; } = NODE_STATE.FOLLOWER;
    public int Id { get; }
    public int? CurrentLeader { get; set; } = null;
    public int ElectionTimeout = 300; // in ms
    public bool HasVoted { get; set; } = false;
    public bool IsRunning = false;
    public INode[] Neighbors { get; set; } = [];
    public int Term { get; set; } = 0;
    private readonly object TimeoutLock = new();
    public int? Vote { get; set; } = null;

    public Node(int id)
    {
        Id = id;
        ResetElectionTimeout();
    }

    public bool AppendEntries(int lId, int lTerm)
    {
        if (lTerm >= Term)
        {
            State = NODE_STATE.FOLLOWER;
            CurrentLeader = lId;
            return true;
        }

        return false;
    }

    public void BecomeCandidate()
    {
        State = NODE_STATE.CANDIDATE;
        Term += 1;
        Vote = Id;
        HasVoted = true;

        GetVotes();
    }

    private void GetVotes()
    {
        int tally = 1;
        int required = (int)Math.Ceiling((Neighbors.Length + 1.0) / 2);
        foreach (INode node in Neighbors)
        {
            bool voted = node.RequestVoteFor(Id, Term);
            if (voted)
            {
                tally++;
            }
        }

        if (tally >= required)
        {
            State = NODE_STATE.LEADER;
        }
    }

    public void Heartbeat(int lId, int lTerm)
    {
        throw new NotImplementedException();
    }

    public bool RequestVoteFor(int cId, int cTerm)
    {
        if (HasVoted && cTerm <= Term) { return false; }

        HasVoted = true;
        Vote = cId;
        return true;
    }

    private void ResetElectionTimeout()
    {
        Random r = new();
        lock (TimeoutLock)
        {
            ElectionTimeout = r.Next(150, 301);
        }
    }

    public Thread Run()
    {
        Thread t = new(() =>
        {
            // If already running, don't run again
            if (IsRunning) { return; }

            IsRunning = true;
            while (IsRunning)
            {
                if (State == NODE_STATE.LEADER)
                {
                    foreach (INode n in Neighbors)
                    {
                        n.Heartbeat(Id, Term);
                    }
                }

                if (State == NODE_STATE.FOLLOWER)
                {
                    lock (TimeoutLock)
                    {
                        ElectionTimeout -= 10;
                    }

                    if (ElectionTimeout <= 0)
                    {
                        ResetElectionTimeout();
                        BecomeCandidate();
                    }

                }

                try
                {
                    Thread.Sleep(State == NODE_STATE.LEADER ? 50 : 10);
                }
                catch (ThreadInterruptedException)
                {
                    IsRunning = false;
                }
            }
        });

        t.Start();
        return t;
    }

    public void Stop()
    {
        IsRunning = false;
    }
}
