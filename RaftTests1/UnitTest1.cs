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
        Node leader = new(0) { State = NODESTATE.LEADER };
        var follower = Substitute.For<INode>();
        leader.Neighbors = [follower];

        // ACT
        Thread t = leader.Run();

        Thread.Sleep(100);
        leader.Stop();
        t.Join();

        // ASSERT
        follower.Received().Heartbeat(leader.Id, leader.Term);
    }

    [Fact]
    public void WhenLeaderActive_SendsHeartBeatEvery50ms()
    {
        // ARRANGE
        Node leader = new(0) { State = NODESTATE.LEADER };
        var follower = Substitute.For<INode>();
        leader.Neighbors = [follower];

        // ACT
        Thread t = leader.Run();

        Thread.Sleep(100);
        leader.Stop();
        t.Join();

        // ASSERT
        follower.Received(2).Heartbeat(leader.Id, leader.Term);
    }

    [Fact]
    // Testing 2
    public void Cluster_WhenNodeReceivesAppendEntries_ThenRemembersOtherNodeIsCurrentLeader()
    {
        Node leader = new(0) { State = NODESTATE.LEADER };
        Node follower = new(1);
        leader.Neighbors = [follower];

        follower.AppendEntries(leader.Id, leader.Term);

        Assert.Equal(leader.Id, follower.CurrentLeader);
    }

    [Fact]
    // Testing 3
    public void SingleNode_WhenInitialized_ShouldBeInFollowerState()
    {
        Node n = new(0);
        Assert.Equal(NODESTATE.FOLLOWER, n.State);
    }

    [Fact]
    // Testing 4
    public void WhenFollowerDoesntGetMessageFor300ms_ItStartsAnElection()
    {
        Node n1 = new(0);
        var n2 = Substitute.For<INode>();
        var n3 = Substitute.For<INode>();
        n2.RequestVoteFor(Arg.Any<int>(), Arg.Any<int>()).Returns(false);
        n3.RequestVoteFor(Arg.Any<int>(), Arg.Any<int>()).Returns(false);
        n1.Neighbors = [n2, n3];

        // ACT
        Thread t = n1.Run();

        Thread.Sleep(300);
        n1.Stop();
        t.Join();

        // ASSERT
        Assert.Equal(NODESTATE.CANDIDATE, n1.State);
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

    [Fact]
    // Testing 7
    public void WhenFollowerGetsAppendEntriesMessage_ItResetsElectionTimer()
    {
        // ARRANGE
        Node follower = new(0);
        var leader = Substitute.For<INode>();
        var n2 = Substitute.For<INode>();

        leader.Id.Returns(0);
        leader.Id.Returns(1);
        follower.Neighbors = [leader, n2];

        // ACT
        Thread t = follower.Run();

        for (int i = 0; i < 4; i++)
        {
            bool worked = follower.AppendEntries(leader.Id, leader.Term);
            Assert.True(worked);
            Thread.Sleep(50);
        }

        follower.Stop();
        t.Join();

        // ASSERT
        Assert.True(follower.State == NODESTATE.FOLLOWER);
    }

    [Fact]
    // Testing 8
    public void SingleNode_WhenItBecomesCandidate_ShouldBecomeLeader()
    {
        Node n = new(0);
        n.BecomeCandidate();
        Assert.Equal(NODESTATE.LEADER, n.State);
    }

    [Fact]
    // Testing 8
    public void Cluster_WhenOneBecomesCandidate_ShouldBecomeLeader()
    {
        Node n1 = new(0);
        var n2 = Substitute.For<INode>();
        var n3 = Substitute.For<INode>();
        n1.Neighbors = [n2, n3];

        n2.RequestVoteFor(Arg.Any<int>(), Arg.Any<int>()).Returns(true);
        n3.RequestVoteFor(Arg.Any<int>(), Arg.Any<int>()).Returns(true);

        n1.BecomeCandidate();

        n2.Received().RequestVoteFor(Arg.Any<int>(), Arg.Any<int>());
        n3.Received().RequestVoteFor(Arg.Any<int>(), Arg.Any<int>());
        Assert.Equal(NODESTATE.LEADER, n1.State);
    }

    [Fact]
    public void NodeVotesForCandidate_WhenVoteRequested()
    {
        Node follower = new(0);
        Node candidate = new(1);
        follower.Neighbors = [candidate];

        follower.RequestVoteFor(candidate.Id, candidate.Term);

        Assert.Equal(candidate.Id, follower.Vote);
        Assert.True(follower.HasVoted);
    }

    [Fact]
    // Testing 9
    public void CandidateReceivesMajorityVotes_WhileWaitinForUnresponsiveNode_StillBecomesLeader()
    {
        Node candidate = new(0);
        var n1 = Substitute.For<INode>();
        var n2 = Substitute.For<INode>();

        n2.When(n => n.RequestVoteFor(Arg.Any<int>(), Arg.Any<int>()))
            .Do(x => candidate.ReceiveVote(true));


        Thread t = candidate.Run();

        Thread.Sleep(300);

        candidate.Stop();
        t.Join();

        Assert.Equal(NODESTATE.LEADER, candidate.State);
    }

    [Fact]
    // Testing 10
    public void GivenAFollowerHasNotVoted_GivenItIsInAnEarlierTerm_WhenARequestRPCIsSent_ThenItRespondsYes()
    {
        Assert.False(true);
        // A follower that has not voted and is in an earlier term responds to a RequestForVoteRPC with yes. (the reply will be a separate RPC)
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
        Assert.Equal(n.Id, n.Vote);
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
        n1.State = NODESTATE.CANDIDATE;

        n1.AppendEntries(n2.Id, n2.Term);

        Assert.Equal(NODESTATE.FOLLOWER, n1.State);
    }

    [Fact]
    // Testing 13
    public void WhenCandidateReceivesMessageFromEqualTerm_BecomesFollower()
    {
        Node n1 = new(0);
        Node n2 = new(1);
        n1.Term = 0;
        n2.Term = 0;
        n1.State = NODESTATE.CANDIDATE;

        n1.AppendEntries(n2.Id, n2.Term);

        Assert.Equal(NODESTATE.FOLLOWER, n1.State);
    }

    [Fact]
    // Testing 14
    public void IfNodeReceivesSecondVoteRequest_ShouldRespondNo()
    {
        Node n1 = new(0);
        Node n2 = new(1);
        n1.Term = 0;
        n1.RequestVoteFor(n2.Id, n2.Term);

        bool actual = n1.RequestVoteFor(n2.Id, n2.Term);

        Assert.False(actual);
    }

    [Fact]
    // Testing 15
    public void IfNodeReceivesSecondVoteRequestForFutureTurm_ShouldRespondYes()
    {
        Node n1 = new(0);
        Node n2 = new(1);
        n1.Term = 0;
        n1.RequestVoteFor(n2.Id, n2.Term);

        n2.Term = 1;
        bool actual = n1.RequestVoteFor(n2.Id, n2.Term);

        Assert.True(actual);
    }

    [Fact]
    // Testing 17
    public void WhenFollowerReceivesAppendEntriesRequest_ItSendsResponse()
    {
        Node n1 = new(0);
        INode n2 = Substitute.For<INode>();

        bool response = n1.AppendEntries(n2.Id, n2.Term);

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

        bool response = n1.AppendEntries(n2.Id, n2.Term);

        Assert.False(response);
    }
}