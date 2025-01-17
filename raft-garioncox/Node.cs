﻿namespace raft_garioncox;

public class Node : INode
{
    public NODESTATE State { get; set; } = NODESTATE.FOLLOWER;
    public int Id { get; }
    public int? CurrentLeader { get; set; } = null;
    public int ElectionTimeout = 300; // in ms
    public bool HasVoted { get; set; } = false;
    public bool IsRunning = false;
    private int Majority => (int)Math.Ceiling((Neighbors.Length + 1.0) / 2);
    public INode[] Neighbors { get; set; } = [];
    public int Term { get; set; } = 0;
    private readonly object TimeoutLock = new();
    private object VoteCountLock = new();
    private int VoteCount = 0;
    public int? Vote { get; set; } = null;

    public Node(int id)
    {
        Id = id;
        ResetElectionTimeout();
    }

    public bool AppendEntries(int leaderId, int leaderTerm)
    {
        if (leaderTerm >= Term)
        {
            State = NODESTATE.FOLLOWER;
            CurrentLeader = leaderId;
            return true;
        }

        return false;
    }

    public void BecomeCandidate()
    {
        State = NODESTATE.CANDIDATE;
        Term += 1;
        Vote = Id;
        HasVoted = true;

        RequestVotes();
    }

    private void RequestVotes()
    {
        int tally = 1;
        foreach (INode node in Neighbors)
        {
            bool voted = node.RequestVoteFor(Id, Term);
            if (voted)
            {
                tally++;
            }
        }

        if (tally >= Majority)
        {
            State = NODESTATE.LEADER;
        }
    }

    public void Heartbeat(int lId, int lTerm)
    {
        throw new NotImplementedException();
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
        INode candidate = Neighbors.Where(n => n.Id == cId).First();

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

    public void RequestVotesRPC()
    {
        foreach (INode n in Neighbors)
        {
            n.RequestVoteForRPC(Id, Term);
        }
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
                if (State == NODESTATE.LEADER)
                {
                    foreach (INode n in Neighbors)
                    {
                        n.Heartbeat(Id, Term);
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
                    Thread.Sleep(State == NODESTATE.LEADER ? 50 : 10);
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
