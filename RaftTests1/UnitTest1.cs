using raft_garioncox;

namespace RaftTests1;

public class RaftTests1
{
    [Fact]
    // Test Case 3
    public void GivenSingleNode_WhenInitialized_ShouldBeInFollowerState()
    {
        Node n = new(0);
        Assert.Equal(NODE_STATE.FOLLOWER, n.State);
    }

    [Fact]
    // Test Case 11
    public void GivenSingleNode_WhenItBecomesCandidate_ShouldVoteForItself()
    {
        Node n = new(0);

        Assert.Equal(0, n.Term);
        Assert.False(n.HasVoted);

        n.SetCandidate();

        Assert.Equal(1, n.Term);
        Assert.Equal(n.Id, n.Vote);
        Assert.True(n.HasVoted);
    }
}