﻿using System.Text.Json;
using raft_garioncox.Records;

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
    public int IntervalScalar { get; set; } = 1;
    public string LogState { get; private set; } = "";
    private int Majority => (int)Math.Ceiling((Neighbors.Keys.Count + 1.0) / 2);
    public Dictionary<int, INode> Neighbors { get; set; } = [];
    public Dictionary<int, int> NextIndexes { get; set; } = [];
    public Dictionary<int, bool> NeighborCommitVote { get; set; } = [];
    public int Term { get; set; } = 0;
    private readonly object TimeoutLock = new();
    private readonly object EntriesLock = new();
    public int TimeoutRate { get; set; } = 10;
    private object VoteCountLock = new();
    private int VoteCount = 0;
    public int? Vote { get; set; } = null;
    public bool IsPaused { get; set; } = false;

    public Node(List<INode> othernNodes)
    {
        foreach (INode node in othernNodes)
        {
            Neighbors[node.Id] = node;
        }

        ResetElectionTimeout();
    }

    public Node()
    {
        ResetElectionTimeout();
    }

    public void BecomeCandidate()
    {
        State = NODESTATE.CANDIDATE;
        Term += 1;
        Vote = Id;
        HasVoted = true;

        RequestVotesRPC();
    }

    public void BecomeLeader()
    {
        State = NODESTATE.LEADER;
        CurrentLeader = Id;
        ResetElectionTimeout(true);
        foreach (int key in Neighbors.Keys)
        {
            NextIndexes[key] = Entries.Count;
        }
        Heartbeat();
    }

    public void CommitEntry()
    {
        if (Entries.Count == 0) { return; }

        LogState = Entries[CommittedLogIndex].Value;
        CommittedLogIndex++;

        foreach (int key in NeighborCommitVote.Keys)
        {
            NeighborCommitVote[key] = false;
        }
    }

    public Task Heartbeat()
    {
        Neighbors.Values.Select(async node =>
        {
            List<Entry> newEntries = [];
            try
            {
                int nextIndex = NextIndexes[node.Id];
                newEntries = Entries.Skip(nextIndex).ToList();
            }
            catch
            { }

            int previousEntryIndex = NextIndexes[node.Id] - 1;
            if (previousEntryIndex < 0)
            {
                previousEntryIndex = 0;
            }

            int previousEntryTerm = 0;

            try
            {
                previousEntryTerm = Entries[previousEntryIndex].Term;
            }
            catch
            { }

            AppendEntriesDTO dto = new(Id, Term, CommittedLogIndex, previousEntryIndex, previousEntryTerm, newEntries);
            await node.RequestAppendEntries(dto);
        }).ToArray();

        return Task.CompletedTask;
    }

    public Task ReceiveCommand(ClientCommandDTO dto)
    {
        Entry e = new(Term, dto.command);
        Entries.Add(e);
        return Task.CompletedTask;
    }

    public Task RespondVote(VoteResponseDTO dto)
    // Follower calls this on a candidate
    {
        if (dto.Vote)
        {
            lock (VoteCountLock)
            {
                VoteCount++;
            }
        }

        TryBecomeLeader();
        return Task.CompletedTask;
    }

    public Task RequestVoteRPC(VoteRequestDTO dto)
    // Candidate calls this function on a follower
    {
        if (IsPaused) { return Task.CompletedTask; }

        INode candidate = Neighbors[dto.Id];

        if (HasVoted && dto.Term <= Term)
        {
            candidate.RespondVote(new VoteResponseDTO(false));
        }
        else
        {
            HasVoted = true;
            Vote = dto.Id;
            candidate.RespondVote(new VoteResponseDTO(true));
        }

        return Task.CompletedTask;
    }

    public void TryBecomeLeader()
    {
        // VoteCount + 1 since we always vote for ourselves
        if (VoteCount + 1 >= Majority)
        {
            lock (VoteCountLock)
            {
                VoteCount = 0;
            }
            BecomeLeader();
        }
    }

    public void RequestVotesRPC()
    {
        VoteRequestDTO dto = new(Id, Term);

        foreach (INode node in Neighbors.Values)
        {
            node.RequestVoteRPC(dto);
        }

        TryBecomeLeader();
    }

    public void ResetElectionTimeout(bool isLeader = false)
    {
        Random r = new();
        lock (TimeoutLock)
        {
            if (isLeader)
            {
                ElectionTimeout = 50 * IntervalScalar;
            }
            else
            {
                ElectionTimeout = r.Next(150 * IntervalScalar, 301 * IntervalScalar);
            }
        }
    }

    public async Task RequestAppendEntries(AppendEntriesDTO dto)
    {
        if (IsPaused) { return; }

        bool didAcceptLogs;

        try
        {
            Entry previousEntry = Entries[dto.PreviousEntryIndex];
            didAcceptLogs = previousEntry.Term == dto.PreviousEntryTerm;

            if (previousEntry.Term > dto.PreviousEntryTerm)
            {
                Entries = Entries.Take(dto.PreviousEntryIndex).ToList();
            }
        }
        catch
        {
            didAcceptLogs = Entries.Count == 0 && dto.PreviousEntryIndex == 0;
        }

        if (dto.LeaderTerm < Term)
        {
            didAcceptLogs = false;
        }

        _ = Neighbors[dto.LeaderId].RespondAppendEntries(new RespondEntriesDTO(Id, Term, CommittedLogIndex, didAcceptLogs));
        if (dto.LeaderTerm < Term) { return; }

        State = NODESTATE.FOLLOWER;
        CurrentLeader = dto.LeaderId;
        CommittedLogIndex = dto.CommittedLogIndex;
        if (CommittedLogIndex > 0)
        {
            LogState = Entries[CommittedLogIndex - 1].Value;
        }
        Term = dto.LeaderTerm;

        ResetElectionTimeout();

        if (dto.Entries.Count != 0 && didAcceptLogs)
        {
            Entries = Entries.Concat(dto.Entries).ToList();
        }

        await Task.CompletedTask;
    }

    public async Task RespondAppendEntries(RespondEntriesDTO dto)
    {
        lock (EntriesLock)
        {
            NeighborCommitVote[dto.FollowerId] = dto.Response;
        }
        if (!dto.Response)
        {
            NextIndexes[dto.FollowerId] = NextIndexes[dto.FollowerId] - 1;
        }
        else
        {
            NextIndexes[dto.FollowerId] = Entries.Count;
        }

        TryCommit();

        await Task.CompletedTask;
    }

    public void TryCommit()
    {
        int tally = 1 + NeighborCommitVote.Values.Count(vote => vote);

        if (tally >= Majority)
        {
            CommitEntry();
            tally = 0;

            foreach (int key in NeighborCommitVote.Keys)
            {
                NeighborCommitVote[key] = false;
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
                            TryBecomeLeader();
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
