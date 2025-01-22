using System.Data;
using NSubstitute;
using raft_garioncox;

namespace RaftSimulationTests;

public class ReplciationTests
{
    [Fact]
    // Testing 2
    public void WhenLeaderReceivesCommandFromClient_ItAppendsEntryToLog()
    {
        string command = DateTime.MaxValue.ToString();
        Node leader = new(0);

        leader.ReceiveClientCommand(command);

        Assert.NotEmpty(leader.Entries);
        Entry e = leader.Entries.First();
        Assert.Equal(leader.Term, e.Term);
        Assert.Equal(command, e.Value);
    }

    [Fact]
    // Testing 3
    public void WhenNodeIsCreated_ItsLogIsEmpty()
    {
        Node node = new(0);
        Assert.Empty(node.Entries);
    }

    [Fact]
    // Testing 5
    public void WhenLeaderWinsElection_InitializesNextIndexForEachFollower()
    {
        var node1 = Substitute.For<INode>();
        var node2 = Substitute.For<INode>();
        Node node3 = new(0)
        {
            Neighbors = new Dictionary<int, INode>
            {
                { 1, node1 },
                { 2, node2 }
            }
        };

        node3.BecomeLeader();

        foreach (var key in node3.Neighbors.Keys)
        {
            Assert.Equal(node3.Entries.Count + 1, node3.NextIndexes[key]);
        }
    }
}