namespace raft_garioncox;

public class Node : INode
{
    public NODESTATE State { get; set; } = NODESTATE.FOLLOWER;
    public int Id { get; set; }
    public int CommittedLogIndex { get; set; } = 0;
    public int? CurrentLeader { get; set; } = null;
    public int ElectionTimeout { get; set; } = 300; // in ms
    public List<Entry> Entries { get; set; } = [];
    public bool HasVoted { get; set; } = false;
    public bool IsRunning = false;
    private int Majority => (int)Math.Ceiling((Neighbors.Keys.Count + 1.0) / 2);
    public Dictionary<int, INode> Neighbors { get; set; } = [];
    public Dictionary<int, int> NextIndexes { get; set; } = [];
    public Dictionary<int, bool> Neighbor_Vote { get; set; } = [];
    public int Term { get; set; } = 0;
    private readonly object TimeoutLock = new();
    public int TimeoutRate { get; set; } = 10;
    public int TimeoutMultiplier { get; set; } = 1;
    private object VoteCountLock = new();
    private int VoteCount = 0;
    public int? Vote { get; set; } = null;
    public bool IsPaused { get; set; } = false;

    public Node(int id)
    {
        Id = id;
        ResetElectionTimeout();
    }

    public async Task AppendEntries(int leaderId, int leaderTerm, int committedLogIndex, Entry? entry = null)
    {
        if (IsPaused) { return; }

        Neighbors[leaderId].ReceiveAppendEntriesResponse(Id, Term, CommittedLogIndex, true);
        if (leaderTerm < Term) { return; }

        State = NODESTATE.FOLLOWER;
        CurrentLeader = leaderId;
        CommittedLogIndex = committedLogIndex;
        Term = leaderTerm;

        ResetElectionTimeout();

        if (entry != null)
        {
            Entries.Add(entry);
        }

        await Task.CompletedTask;
    }

    public async Task ReceiveAppendEntriesResponse(int followerId, int followerTerm, int followerEntryIndex, bool response)
    {
        // if (response == false) {return;}
        Neighbor_Vote[followerId] = response;

        int tally = 1;
        foreach (bool vote in Neighbor_Vote.Values)
        {
            if (vote)
            {
                tally++;
            }

        }

        if (tally >= Majority)
        {
            CommittedLogIndex++;
        }
    }

    public void BecomeCandidate()
    {
        State = NODESTATE.CANDIDATE;
        Term += 1;
        Vote = Id;
        HasVoted = true;

        RequestVotes();
    }

    public void BecomeLeader()
    {
        State = NODESTATE.LEADER;
        CurrentLeader = Id;
        foreach (int key in Neighbors.Keys)
        {
            Neighbors[key].AppendEntries(Id, Term, CommittedLogIndex);
            NextIndexes[key] = Entries.Count + 1;
        }
    }

    public void CommitEntry()
    {
        CommittedLogIndex++;
    }

    public Task Heartbeat()
    {
        Neighbors.Values.Select(async node =>
        {
            Entry? e = Entries.Count > 0 ? Entries.Last() : null;
            await node.AppendEntries(Id, Term, CommittedLogIndex, e);
        }).ToArray();
        return Task.CompletedTask;
    }

    public void ReceiveClientCommand(string command)
    {
        Entry e = new(Term, command);
        Entries.Add(e);
    }

    public void ReceiveVote(bool vote)
    {
        lock (VoteCountLock)
        {
            VoteCount++;
        }
    }

    public bool RequestVoteFor(int cId, int cTerm)
    {
        if (HasVoted && cTerm <= Term) { return false; }

        HasVoted = true;
        Vote = cId;
        return true;
    }

    public Task RequestVoteForRPC(int cId, int cTerm)
    {
        INode candidate = Neighbors[cId];

        if (HasVoted && cTerm <= Term)
        {
            candidate.ReceiveVote(false);
        }
        else
        {
            HasVoted = true;
            Vote = cId;
            candidate.ReceiveVote(true);
        }

        return Task.CompletedTask;
    }

    private void RequestVotes()
    {
        int tally = 1;
        foreach (INode node in Neighbors.Values)
        {
            bool voted = node.RequestVoteFor(Id, Term);
            if (voted)
            {
                tally++;
            }
        }

        if (tally >= Majority)
        {
            BecomeLeader();
        }
    }

    public void RequestVotesRPC()
    {
        foreach (INode node in Neighbors.Values)
        {
            node.RequestVoteForRPC(Id, Term);
        }
    }

    public void ResetElectionTimeout(bool isLeader = false)
    {
        Random r = new();
        lock (TimeoutLock)
        {
            if (isLeader)
            {
                ElectionTimeout = 50 * TimeoutMultiplier;
            }
            else
            {
                ElectionTimeout = r.Next(150 * TimeoutMultiplier, 301 * TimeoutMultiplier);
            }
        }
    }

    public Thread Run()
    {
        Thread t = new(async () =>
        {
            if (IsRunning) { return; }

            IsRunning = true;
            while (IsRunning)
            {
                if (!IsPaused)
                {
                    if (State == NODESTATE.LEADER && ElectionTimeout <= 0)
                    {
                        await Heartbeat();

                        if (ElectionTimeout <= 0)
                        {
                            ResetElectionTimeout(true);
                        }
                    }

                    else
                    {
                        if (State == NODESTATE.CANDIDATE)
                        {
                            if (VoteCount >= Majority)
                            {
                                State = NODESTATE.LEADER;
                            }
                        }

                        if (ElectionTimeout <= 0)
                        {
                            ResetElectionTimeout();
                            BecomeCandidate();
                        }
                    }

                    lock (TimeoutLock)
                    {
                        ElectionTimeout -= TimeoutRate;
                    }
                }

                try
                {
                    Thread.Sleep(TimeoutRate);
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

    public void Pause()
    {
        IsPaused = true;
    }

    public void Unpause()
    {
        IsPaused = false;
        ResetElectionTimeout(State == NODESTATE.LEADER);
    }
}
