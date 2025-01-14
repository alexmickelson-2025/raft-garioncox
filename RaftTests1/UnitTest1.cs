using NSubstitute;
using raft_garioncox;

namespace RaftTests1;

public class RaftTests1
{
    [Fact]
    // Test Case 3
    public void SingleNode_WhenInitialized_ShouldBeInFollowerState()
    {
        Node n = new(0);
        Assert.Equal(NODE_STATE.FOLLOWER, n.State);
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
        Assert.Equal(n.Id, n.Vote);
        Assert.True(n.HasVoted);
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

        n2.RequestVote(Arg.Is<INode>(x => x == n1)).Returns(true);
        n3.RequestVote(Arg.Is<INode>(x => x == n1)).Returns(true);


        n1.SetCandidate();

        n2.Received().RequestVote(Arg.Is<INode>(x => x == n1));
        n3.Received().RequestVote(Arg.Is<INode>(x => x == n1));
        Assert.Equal(NODE_STATE.LEADER, n1.State);
    }

}