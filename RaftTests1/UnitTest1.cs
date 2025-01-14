using NSubstitute;
using raft_garioncox;

namespace RaftTests1;

public class RaftTests1
{
    [Fact]
    // Test Case 1
    public void WhenLeaderActive_SendsHeartbeatWithin50ms()
    {
        // ARRANGE
        Node leader = new(0) { State = NODE_STATE.LEADER };
        var follower = Substitute.For<INode>();
        leader.Neighbors = [follower];

        // ACT
        Thread t = new(() => leader.Run());
        t.Start();

        Thread.Sleep(50);
        t.Interrupt();

        // ASSERT
        follower.Received().Heartbeat(Arg.Any<INode>());
    }

    [Fact]
    // Test Case 3
    public void SingleNode_WhenInitialized_ShouldBeInFollowerState()
    {
        Node n = new(0);
        Assert.Equal(NODE_STATE.FOLLOWER, n.State);
    }

    [Fact]
    // Test Case 8
    public void SingleNode_WhenItBecomesCandidate_ShouldBecomeLeader()
    {
        Node n = new(0);
        n.SetCandidate();
        Assert.Equal(NODE_STATE.LEADER, n.State);
    }

    [Fact]
    // Test Case 8
    public void Cluster_WhenOneBecomesCandidate_ShouldBecomeLeader()
    {
        Node n1 = new(0);
        var n2 = Substitute.For<INode>();
        var n3 = Substitute.For<INode>();
        n1.Neighbors = [n2, n3];

        n2.RequestToVoteFor(Arg.Is<INode>(x => x == n1)).Returns(true);
        n3.RequestToVoteFor(Arg.Is<INode>(x => x == n1)).Returns(true);

        n1.SetCandidate();

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
    // Test Case 11
    public void SingleNode_WhenItBecomesCandidate_ShouldVoteForItself()
    {
        Node n = new(0);

        Assert.Equal(0, n.Term);
        Assert.False(n.HasVoted);

        n.SetCandidate();

        Assert.Equal(1, n.Term);
        Assert.Equal(n, n.Vote);
        Assert.True(n.HasVoted);
    }

    [Fact]
    // Test Case 12
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
    // Test Case 13
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
    // Test Case 14
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
    // Test Case 15
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
    // Test Case 17
    public void WhenFollowerReceivesAppendEntriesRequest_ItSendsResponse()
    {
        Node n1 = new(0);
        INode n2 = Substitute.For<INode>();

        bool response = n1.AppendEntries(n2);

        Assert.True(response);
    }

    [Fact]
    // Test Case 18
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