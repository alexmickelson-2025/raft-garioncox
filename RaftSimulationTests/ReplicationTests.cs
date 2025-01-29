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
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);

        var node2 = Substitute.For<INode>();
        node2.Id.Returns(2);

        Node leader = new(0)
        {
            Neighbors = new Dictionary<int, INode>() {
                {1, follower},
                {2, node2}
            }
        };

        string command = DateTime.MaxValue.ToString();

        // ACT
        leader.ReceiveClientCommand(command);
        leader.Heartbeat();

        // ASSERT
        follower.Received().AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>());
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
        var follower = Substitute.For<INode>();
        var node2 = Substitute.For<INode>();
        Node node3 = new(0)
        {
            Neighbors = new Dictionary<int, INode>
            {
                { 1, follower },
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

        follower.Received(1).AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>());
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

        await follower.AppendEntries(mockNode.Id, mockNode.Term, mockNode.CommittedLogIndex, 0, 0, []);

        Assert.Equal(mockNode.CommittedLogIndex, follower.CommittedLogIndex);
    }

    [Fact]
    // Test 8
    public async Task WhenLeaderReceivesMajorityResponsesForLog_ItCommitsIt()
    {
        var mockfollower = Substitute.For<INode>();
        mockfollower.Id.Returns(1);
        mockfollower.CommittedLogIndex.Returns(1);
        mockfollower.Term.Returns(0);
        var mockNode2 = Substitute.For<INode>();
        mockNode2.Id.Returns(2);
        Node leader = new(0)
        {
            Neighbors = new Dictionary<int, INode>() { { mockfollower.Id, mockfollower }, { mockNode2.Id, mockNode2 } },
            Entries = [new Entry(1, "command")]
        };

        await leader.ReceiveAppendEntriesResponse(mockfollower.Id, mockfollower.Term, mockfollower.CommittedLogIndex, true);

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

        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 0, 0, [e]);

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

        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 0, 0, []);

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

        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 0, 0, []);

        Assert.Equal(leader.CommittedLogIndex, follower.CommittedLogIndex);
    }

    [Fact]
    // Test 15a
    public void LeaderIncludesIndexAndTerm_OfEntryPrecedingNewEntry_WhenSendingAppendEntries()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        Node leader = new(0)
        {
            Neighbors = new Dictionary<int, INode>() { { follower.Id, follower } }
        };

        leader.Heartbeat();

        follower.Received().AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>());
    }

    [Fact]
    // Test 15b
    public async Task WhenFollowerReceivesAppendEntries_AndDoesNotFindEntryInItsLogWithSameIndexAndTermInParams_ItRefusesNewEntries()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        Node follower = new(0)
        {
            Neighbors = new Dictionary<int, INode>() { { leader.Id, leader } }
        };

        int previousEntryIndex = 1;
        int previousEntryTerm = 2;
        List<Entry> newEntries = [
            new Entry(1,"command"),
            new Entry(2, "command2"),
        ];

        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, previousEntryIndex, previousEntryTerm, newEntries);

        await leader.Received().ReceiveAppendEntriesResponse(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), false);
        Assert.Empty(follower.Entries);
    }

    [Fact]
    // Test 15c
    public async Task WhenFollowerReceivesAppendEntries_AndFindsEntryInItsLogWithNewerTermInParams_ItAcceptsNewEntries()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        List<Entry> newEntries = [
            new Entry(3,"command2"),
            new Entry(4, "command3"),
        ];

        Node follower = new(0)
        {
            Neighbors = new Dictionary<int, INode>() { { leader.Id, leader } },
            Entries = [
                new Entry(1,"command"),
                new Entry(2, "command2"),
            ]
        };

        int previousEntryIndex = 1;
        int previousEntryTerm = 2;

        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, previousEntryIndex, previousEntryTerm, newEntries);

        await leader.Received().ReceiveAppendEntriesResponse(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), true);
        Assert.Equal(newEntries[0].Value, follower.Entries[2].Value);
        Assert.Equal(newEntries[1].Value, follower.Entries[3].Value);
    }

    // [Fact]
    // // Test 15d
    // public async Task WhenFollowerReceivesAppendEntries_WhenLeaderPreviousLogIndexGreaterThanCurrentIndex_LeaderDecreasesPreviousTermEntry()
    // {
    //     // - if index is greater, it will be decreased by leader
    // }

    [Fact]
    // Test 16
    public void WhenLeaderSendsHeartbeatWithLog_DoesNotReceiveMajority_LogRemainsUncommitted()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        var node2 = Substitute.For<INode>();
        node2.Id.Returns(2);
        Node leader = new(0)
        {
            Neighbors = new Dictionary<int, INode>() {
                {follower.Id, follower},
                {node2.Id, node2}
            },
            Entries = [new Entry(1, "command")],
        };

        leader.Heartbeat();

        Assert.Equal(0, leader.CommittedLogIndex);
    }

    [Fact]
    // Test 17
    public void WhenLeader_IfNoResponseFromFollower_LeaderContinuesToSendLogEntriesInHeartbeats()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        Node leader = new(0)
        {
            Entries = [new Entry(1, "command")],
            Neighbors = new Dictionary<int, INode>() {
                {follower.Id, follower}
            }
        };

        leader.Heartbeat();
        leader.Heartbeat();

        follower.Received(2).AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>());
    }

    // [Fact]
    // public async Task WhenLeader_IfResponseFromFollower_LeaderDoesNotIncludeLogEntriesInHeartbeats()
    // {
    //     Node leader = new(0)
    //     {
    //         Entries = [new Entry(1, "command")],
    //     };
    //     var follower = Substitute.For<INode>();
    //     follower.AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),Arg.Any<int>(),Arg.Any<int>(), Arg.Any<List<Entry>>())
    //         .Returns(n => leader.ReceiveAppendEntriesResponse(follower.Id, follower.Term, follower.CommittedLogIndex, true));
    //     leader.Neighbors = new Dictionary<int, INode>() { { follower.Id, follower } };

    //     await leader.Heartbeat();
    //     await leader.Heartbeat();

    //     await follower.Received(1).AppendEntries(Arg.Any<int>(), Arg.Any<int>(), 0, Arg.Any<List<Entry>>());
    //     await follower.Received(1).AppendEntries(Arg.Any<int>(), Arg.Any<int>(), 1, Arg.Any<List<Entry>>());
    // }
}