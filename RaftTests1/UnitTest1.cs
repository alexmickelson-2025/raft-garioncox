using System.Diagnostics;
using NSubstitute;
using raft_garioncox;

namespace RaftTests1;

public class RaftTests1
{
    [Fact]
    // Testing 1
    public void WhenLeaderActive_SendsHeartbeatWithin50ms()
    {
        // ARRANGE
        Node leader = new(0) { State = NODE_STATE.LEADER };
        var follower = Substitute.For<INode>();
        leader.Neighbors = [follower];

        // ACT
        Thread t = leader.Run();

        Thread.Sleep(100);
        leader.Stop();
        t.Join();

        // ASSERT
        follower.Received().Heartbeat(Arg.Any<INode>());
    }

    [Fact]
    public void WhenLeaderActive_SendsHeartBeatEvery50ms()
    {
        // ARRANGE
        Node leader = new(0) { State = NODE_STATE.LEADER };
        var follower = Substitute.For<INode>();
        leader.Neighbors = [follower];

        // ACT
        Thread t = leader.Run();

        Thread.Sleep(100);
        leader.Stop();
        t.Join();

        // ASSERT
        follower.Received(2).Heartbeat(Arg.Any<INode>());
    }

    [Fact]
    // Testing 2
    public void Cluster_WhenNodeReceivesAppendEntries_ThenRemembersOtherNodeIsCurrentLeader()
    {
        Node leader = new(0) { State = NODE_STATE.LEADER };
        Node follower = new(1);
        leader.Neighbors = [follower];

        follower.AppendEntries(leader);

        Assert.Equal(leader, follower.CurrentLeader);
    }

    [Fact]
    // Testing 3
    public void SingleNode_WhenInitialized_ShouldBeInFollowerState()
    {
        Node n = new(0);
        Assert.Equal(NODE_STATE.FOLLOWER, n.State);
    }

    [Fact]
    // Testing 4
    public void WhenFollowerDoesntGetMessageFor300ms_ItStartsAnElection()
    {
        Node n1 = new(0);
        var n2 = Substitute.For<INode>();
        var n3 = Substitute.For<INode>();
        n2.RequestToVoteFor(Arg.Any<INode>()).Returns(false);
        n3.RequestToVoteFor(Arg.Any<INode>()).Returns(false);
        n1.Neighbors = [n2, n3];

        // ACT
        Thread t = n1.Run();

        Thread.Sleep(300);
        n1.Stop();
        t.Join();

        // ASSERT
        Assert.Equal(NODE_STATE.CANDIDATE, n1.State);
    }

    [Fact]
    public void ElectionTimeInitialized_WithRandomValueBetween150and300ms()
    {
        int previousTimeout = 0;
        int matches = 0;
        for (int i = 0; i < 100; i++)
        {
            Node n1 = new(0);
            if (previousTimeout == n1.ElectionTimeout)
            {
                matches++;
            }

            previousTimeout = n1.ElectionTimeout;

            Assert.InRange(n1.ElectionTimeout, 150, 300);
        }

        Assert.True(matches < 20);
    }

    [Fact]
    // Testing 5
    public void ElectionTimeReset_WithRandomValueBetween150and300ms()
    {
        Node n1 = new(0);
        n1.ElectionTimeout = 10;

        Thread t = n1.Run();

        Thread.Sleep(10);
        n1.Stop();
        t.Join();

        Assert.InRange(n1.ElectionTimeout, 150, 300);
    }

    [Fact]
    // Testing 6
    public void WhenNewElectionBegins_TermIsIncrementedBy1()
    {
        Node n = new(0);
        int previousTerm = n.Term;

        Thread t = n.Run();

        Thread.Sleep(300);
        n.Stop();
        t.Join();

        int newTerm = n.Term;

        Assert.True(newTerm > previousTerm);
    }

    // [Fact]
    // // Testing 7
    // public void WhenFollowerGetsAppendEntriesMessage_ItResetsElectionTimer()
    // {
    //     // (i.e. it doesn't start an election even after more than 300ms)
    //     Node follower = new(0);
    //     Node leader = new(1);
    //     Node n2 = new(2);
    //     follower.Term = 0;
    //     leader.Term = 1;
    //     follower.Neighbors = [leader, n2];

    //     Thread t = follower.Run();

    //     for (int i = 0; i < 4; i++)
    //     {
    //         bool worked = follower.AppendEntries(leader);
    //         Assert.True(worked);
    //         Thread.Sleep(50);
    //     }

    //     follower.Stop();
    //     t.Join();

    //     Assert.True(follower.State == NODE_STATE.FOLLOWER);
    //     Assert.True(false);
    // }

    [Fact]
    // Testing 8
    public void SingleNode_WhenItBecomesCandidate_ShouldBecomeLeader()
    {
        Node n = new(0);
        n.BecomeCandidate();
        Assert.Equal(NODE_STATE.LEADER, n.State);
    }

    [Fact]
    // Testing 8
    public void Cluster_WhenOneBecomesCandidate_ShouldBecomeLeader()
    {
        Node n1 = new(0);
        var n2 = Substitute.For<INode>();
        var n3 = Substitute.For<INode>();
        n1.Neighbors = [n2, n3];

        n2.RequestToVoteFor(Arg.Is<INode>(x => x == n1)).Returns(true);
        n3.RequestToVoteFor(Arg.Is<INode>(x => x == n1)).Returns(true);

        n1.BecomeCandidate();

        n2.Received().RequestToVoteFor(Arg.Is<INode>(x => x == n1));
        n3.Received().RequestToVoteFor(Arg.Is<INode>(x => x == n1));
        Assert.Equal(NODE_STATE.LEADER, n1.State);
    }

    [Fact]
    public void NodeVotesForCandidate_WhenVoteRequested()
    {
        Node follower = new(0);
        Node candidate = new(1);
        follower.Neighbors = [candidate];

        follower.RequestToVoteFor(candidate);

        Assert.Equal(candidate, follower.Vote);
        Assert.True(follower.HasVoted);
    }

    [Fact]
    // Testing 11
    public void SingleNode_WhenItBecomesCandidate_ShouldVoteForItself()
    {
        Node n = new(0);

        Assert.Equal(0, n.Term);
        Assert.False(n.HasVoted);

        n.BecomeCandidate();

        Assert.Equal(1, n.Term);
        Assert.Equal(n, n.Vote);
        Assert.True(n.HasVoted);
    }

    [Fact]
    // Testing 12
    public void WhenCandidateReceivesMessageFromLaterTerm_BecomesFollower()
    {
        Node n1 = new(0);
        Node n2 = new(1);
        n1.Term = 0;
        n2.Term = 1;
        n1.State = NODE_STATE.CANDIDATE;

        n1.AppendEntries(n2);

        Assert.Equal(NODE_STATE.FOLLOWER, n1.State);
    }

    [Fact]
    // Testing 13
    public void WhenCandidateReceivesMessageFromEqualTerm_BecomesFollower()
    {
        Node n1 = new(0);
        Node n2 = new(1);
        n1.Term = 0;
        n2.Term = 0;
        n1.State = NODE_STATE.CANDIDATE;

        n1.AppendEntries(n2);

        Assert.Equal(NODE_STATE.FOLLOWER, n1.State);
    }

    [Fact]
    // Testing 14
    public void IfNodeReceivesSecondVoteRequest_ShouldRespondNo()
    {
        Node n1 = new(0);
        Node n2 = new(1);
        n1.Term = 0;
        n1.RequestToVoteFor(n2);

        bool actual = n1.RequestToVoteFor(n2);

        Assert.False(actual);
    }

    [Fact]
    // Testing 15
    public void IfNodeReceivesSecondVoteRequestForFutureTurm_ShouldRespondYes()
    {
        Node n1 = new(0);
        Node n2 = new(1);
        n1.Term = 0;
        n1.RequestToVoteFor(n2);

        n2.Term = 1;
        bool actual = n1.RequestToVoteFor(n2);

        Assert.True(actual);
    }

    [Fact]
    // Testing 17
    public void WhenFollowerReceivesAppendEntriesRequest_ItSendsResponse()
    {
        Node n1 = new(0);
        INode n2 = Substitute.For<INode>();

        bool response = n1.AppendEntries(n2);

        Assert.True(response);
    }

    [Fact]
    // Testing 18
    public void WhenFollowerReceivesAppendEntriesRequest_WithPreviousTerm_ItRejects()
    {
        Node n1 = new(0);
        INode n2 = Substitute.For<INode>();
        n1.Term = 1;
        n2.Term = 0;

        bool response = n1.AppendEntries(n2);

        Assert.False(response);
    }
}