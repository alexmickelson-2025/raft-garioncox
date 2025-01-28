using System.Data;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.Core.Arguments;
using raft_garioncox;

namespace RaftSimulationTests;

public class ReplciationTests
{
    [Fact]
    // Test 1
    public void WhenLeaderReceivesClientCommand_LeaderSendsLogInNextAppendRPC_ToAllNodes()
    {
        // ARRANGE
        var node1 = Substitute.For<INode>();
        node1.Id.Returns(1);

        var node2 = Substitute.For<INode>();
        node2.Id.Returns(2);

        Node leader = new(0)
        {
            Neighbors = new Dictionary<int, INode>() {
                {1, node1},
                {2, node2}
            }
        };

        string command = DateTime.MaxValue.ToString();

        // ACT
        leader.ReceiveClientCommand(command);
        leader.Heartbeat();

        // ASSERT
        node1.Received().AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, Arg.Any<Entry>());
    }

    [Fact]
    // Test 2
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
    // Test 3
    public void WhenNodeIsCreated_ItsLogIsEmpty()
    {
        Node node = new(0);
        Assert.Empty(node.Entries);
    }

    [Fact]
    // Test 5
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

    [Fact]
    // Test 6
    public void HighestCommittedIndex_IncludedInLeaderAppendEntries()
    {
        Entry e = new(1, "commandstring");
        var follower = Substitute.For<INode>();
        Node leader = new(0)
        {
            Entries = [e],
            CommittedLogIndex = 1,
            Neighbors = new Dictionary<int, INode>
            {
                { 1, follower },
            }
        };

        leader.Heartbeat();

        follower.Received(1).AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, e);
    }

    [Fact]
    // Test 7
    public async Task WhenFollowerLearnsLogIsCommitted_ItCommitsThatLog()
    {
        var mockNode = Substitute.For<INode>();
        Node follower = new(0)
        {
            Neighbors = new Dictionary<int, INode>() { { mockNode.Id, mockNode } }
        };

        await follower.AppendEntries(mockNode.Id, mockNode.Term, mockNode.CommittedLogIndex);

        Assert.Equal(mockNode.CommittedLogIndex, follower.CommittedLogIndex);
    }

    [Fact]
    // Test 8
    public async Task WhenLeaderReceivesMajorityResponsesForLog_ItCommitsIt()
    {
        var mockNode1 = Substitute.For<INode>();
        mockNode1.Id.Returns(1);
        mockNode1.CommittedLogIndex.Returns(1);
        mockNode1.Term.Returns(0);
        var mockNode2 = Substitute.For<INode>();
        mockNode2.Id.Returns(2);
        Node leader = new(0)
        {
            Neighbors = new Dictionary<int, INode>() { { mockNode1.Id, mockNode1 }, { mockNode2.Id, mockNode2 } },
            Entries = [new Entry(1, "command")]
        };

        await leader.ReceiveAppendEntriesResponse(mockNode1.Id, mockNode1.Term, mockNode1.CommittedLogIndex, true);

        Assert.Equal(1, leader.CommittedLogIndex);
    }

    [Fact]
    // Test 9
    public void LeaderCommitsLogs_ByIncrementingCommittedLogIndex()
    {
        Node leader = new(0)
        {
            Entries = [new Entry(1, "name")]
        };

        leader.CommitEntry();

        Assert.Equal(1, leader.CommittedLogIndex);
    }

    [Fact]
    // Test 10
    public async Task GivenFollowerReceivesAppendEntriesWithLogs_ItAppendsItToItsPersonalLog()
    {
        Entry e = new(1, "commandstring");
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        Node follower = new(0)
        {
            Neighbors = new Dictionary<int, INode>() { { leader.Id, leader } }
        };

        await follower.AppendEntries(leader.Id, leader.Term, leader.ElectionTimeout, e);

        Assert.NotEmpty(follower.Entries);
        Assert.Equal(e, follower.Entries.First());
    }

    [Fact]
    // Test 11
    public async Task FollowerRespondsToAppendEntries_WithTermAndLogEntryIndex()
    {
        Entry e = new(1, "commandstring");
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        Node follower = new(0)
        {
            Neighbors = new Dictionary<int, INode>
            {
                { 1, leader },
            }
        };

        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex);

        await leader.Received(1).ReceiveAppendEntriesResponse(follower.Id, follower.Term, follower.CommittedLogIndex, Arg.Any<bool>());
    }

    [Fact]
    // Test 13
    public void WhenLeaderCommitsLog_ItAppliesItToItsStateMachine()
    {
        Entry entry = new(1, "command");
        Node leader = new(0)
        {
            Entries = [entry]
        };

        leader.CommitEntry();

        Assert.Equal(entry.Value, leader.LogState);
    }

    [Fact]
    // Test 14
    public async Task WhenFollowerReceivesHeartbeat_ItMatchesCommitIndexOfHeartbeat()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        leader.CommittedLogIndex.Returns(3);
        Node follower = new(0)
        {
            Neighbors = new Dictionary<int, INode> { { 1, leader }, }
        };

        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex);

        Assert.Equal(leader.CommittedLogIndex, follower.CommittedLogIndex);
    }

    [Fact]
    // Test 16
    public void WhenLeaderSendsHeartbeatWithLog_DoesNotReceiveMajority_LogRemainsUncommitted()
    {
        var node1 = Substitute.For<INode>();
        node1.Id.Returns(1);
        var node2 = Substitute.For<INode>();
        node2.Id.Returns(2);
        Node leader = new(0)
        {
            Neighbors = new Dictionary<int, INode>() {
                {node1.Id, node1},
                {node2.Id, node2}
            },
            Entries = [new Entry(1, "command")],
        };

        leader.Heartbeat();

        Assert.Equal(0, leader.CommittedLogIndex);
    }
}