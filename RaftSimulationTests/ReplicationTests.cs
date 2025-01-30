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
        var client = Substitute.For<IClient>();
        client.Id.Returns(0);

        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);

        var node2 = Substitute.For<INode>();
        node2.Id.Returns(2);

        Node leader = new([follower, node2]) { Id = 0 };

        string command = DateTime.MaxValue.ToString();

        // ACT
        leader.ReceiveCommand(client, command);
        leader.Heartbeat();

        // ASSERT
        follower.Received().RequestAppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>());
    }

    [Fact]
    // Test 2
    public void WhenLeaderReceivesCommandFromClient_ItAppendsEntryToLog()
    {
        var client = Substitute.For<IClient>();
        client.Id.Returns(0);

        string command = DateTime.MaxValue.ToString();
        Node leader = new();

        leader.ReceiveCommand(client, command);

        Assert.NotEmpty(leader.Entries);
        Entry e = leader.Entries.First();
        Assert.Equal(leader.Term, e.Term);
        Assert.Equal(command, e.Value);
    }

    [Fact]
    // Test 3
    public void WhenNodeIsCreated_ItsLogIsEmpty()
    {
        Node node = new();
        Assert.Empty(node.Entries);
    }

    [Fact]
    // Test 4
    public void WhenLeaderWinsElection_InitializesNextIndexForEachFollower()
    {
        var follower = Substitute.For<INode>();
        var node2 = Substitute.For<INode>();
        Node node3 = new([follower, node2]) { Id = 0 };

        node3.BecomeLeader();

        foreach (var key in node3.Neighbors.Keys)
        {
            Assert.Equal(node3.Entries.Count, node3.NextIndexes[key]);
        }
    }

    [Fact]
    // Test 6
    public void HighestCommittedIndex_IncludedInLeaderAppendEntries()
    {
        Entry e = new(1, "commandstring");
        var follower = Substitute.For<INode>();
        Node leader = new([follower])
        {
            Entries = [e],
            CommittedLogIndex = 1,
            Id = 0
        };

        leader.Heartbeat();

        follower.Received(1).RequestAppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>());
    }

    [Fact]
    // Test 7
    public async Task WhenFollowerLearnsLogIsCommitted_ItCommitsThatLog()
    {
        var mockNode = Substitute.For<INode>();
        Node follower = new([mockNode]) { Id = 0 };

        await follower.RequestAppendEntries(mockNode.Id, mockNode.Term, mockNode.CommittedLogIndex, 0, 0, []);

        Assert.Equal(mockNode.CommittedLogIndex, follower.CommittedLogIndex);
    }

    [Fact]
    // Test 8
    public async Task WhenLeaderReceivesMajorityResponsesForLog_ItCommitsIt()
    {
        var mockClient = Substitute.For<IClient>();
        mockClient.Id.Returns(0);

        var mockfollower = Substitute.For<INode>();
        mockfollower.Id.Returns(1);
        mockfollower.CommittedLogIndex.Returns(1);
        mockfollower.Term.Returns(0);

        var mockNode2 = Substitute.For<INode>();
        mockNode2.Id.Returns(2);

        Node leader = new([mockfollower, mockNode2]) { Id = 0 };

        leader.ReceiveCommand(mockClient, "a");

        await leader.RespondAppendEntries(mockfollower.Id, mockfollower.Term, mockfollower.CommittedLogIndex, true);

        Assert.Equal(1, leader.CommittedLogIndex);
    }

    [Fact]
    // Test 9
    public void LeaderCommitsLogs_ByIncrementingCommittedLogIndex()
    {
        var mockClient = Substitute.For<IClient>();
        mockClient.Id.Returns(0);

        Node leader = new() { Id = 0 };

        leader.ReceiveCommand(mockClient, "a");
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
        Node follower = new([leader]) { Id = 0 };

        await follower.RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 0, 0, [e]);

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
        Node follower = new([leader]) { Id = 0 };

        await follower.RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 0, 0, []);

        await leader.Received(1).RespondAppendEntries(follower.Id, follower.Term, follower.CommittedLogIndex, Arg.Any<bool>());
    }

    [Fact]
    // Test 12
    public async Task WhenLeaderReceivesMajorityResponseForAppendEntries_ItSendsConfirmationToClient()
    {
        var client = Substitute.For<IClient>();
        client.Id.Returns(0);

        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        follower.Entries.Returns([new Entry(0, "a")]);

        var follower2 = Substitute.For<INode>();
        follower2.Id.Returns(2);

        Node leader = new([follower, follower2]) { Id = 0 };

        leader.ReceiveCommand(client, "a");
        await leader.RespondAppendEntries(follower.Id, follower.Term, follower.Entries.Count, true);

        await client.Received().ReceiveLeaderCommitResponse("a", true);
    }

    [Fact]
    // Test 13
    public void WhenLeaderCommitsLog_ItAppliesItToItsStateMachine()
    {
        var mockClient = Substitute.For<IClient>();
        mockClient.Id.Returns(0);

        string entry = "a";
        Node leader = new();

        leader.ReceiveCommand(mockClient, entry);
        leader.CommitEntry();

        Assert.Equal(entry, leader.LogState);
    }

    [Fact]
    // Test 14
    public async Task WhenFollowerReceivesHeartbeat_ItMatchesCommitIndexOfHeartbeat()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        leader.CommittedLogIndex.Returns(3);
        Node follower = new([leader]) { Id = 0 };

        await follower.RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 0, 0, []);

        Assert.Equal(leader.CommittedLogIndex, follower.CommittedLogIndex);
    }

    [Fact]
    // Test 15a
    public void LeaderIncludesIndexAndTerm_OfEntryPrecedingNewEntry_WhenSendingAppendEntries()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        Node leader = new([follower]) { Id = 0 };

        leader.Heartbeat();

        follower.Received().RequestAppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>());
    }

    [Fact]
    // Test 15b
    public async Task WhenFollowerReceivesAppendEntries_AndDoesNotFindIndexAndTermMatch_ItRefusesNewEntries()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        Node follower = new([leader])
        {
            Id = 0,
            Entries = [new Entry(0, "a"), new Entry(1, "b")]
        };

        int previousEntryIndex = 1;
        int previousEntryTerm = 2;
        List<Entry> newEntries = [
            new Entry(2, "c"),
            new Entry(2, "d"),
        ];

        await follower.RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, previousEntryIndex, previousEntryTerm, newEntries);

        await leader.Received().RespondAppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), false);
        Assert.Equal(2, follower.Entries.Count);
    }

    [Fact]
    // Test 15c
    public async Task WhenFollowerReceivesAppendEntries_AndFindsEntryInItsLogWithNewerTermInParams_ItAcceptsNewEntries()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        Node follower = new([leader])
        {
            Id = 0,
            Entries = [new Entry(0, "a"), new Entry(1, "b")]
        };

        int previousEntryIndex = 1;
        int previousEntryTerm = 1;
        List<Entry> newEntries = [
            new Entry(1, "c"),
            new Entry(2, "d"),
        ];

        await follower.RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, previousEntryIndex, previousEntryTerm, newEntries);

        await leader.Received().RespondAppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), true);
        Assert.Equal(newEntries[0].Value, follower.Entries[2].Value);
        Assert.Equal(newEntries[1].Value, follower.Entries[3].Value);
    }

    [Fact]
    // Test 15d
    public async Task WhenFollowerReceivesAppendEntries_WhenLeaderPreviousLogIndexGreaterThanCurrentIndex_LeaderDecreasesPreviousTermEntry()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        follower.Entries.Returns([new Entry(0, "a")]);

        var follower2 = Substitute.For<INode>();
        follower2.Id.Returns(2);

        Node leader = new([follower, follower2])
        {
            Id = 0,
            Entries = [new Entry(0, "a"), new Entry(1, "b")]
        };

        leader.BecomeLeader();
        Assert.Equal(2, leader.NextIndexes[follower.Id]);

        await leader.RespondAppendEntries(follower.Id, follower.Term, follower.Entries.Count, false);
        Assert.Equal(1, leader.NextIndexes[follower.Id]);
    }

    [Fact]
    // Test 15e
    public async Task WhenFollowerReceivesAppendEntries_WhenPreviousTermLessThanStoredTerm_ItDeletesAllEntriesAfterInconsistentLog()
    {
        // ARRANGE
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);

        Node follower = new([leader])
        {
            Id = 0,
            Entries = [
                new Entry(1,"a"),
                new Entry(2, "b"), // Inconsistent log
                new Entry(3, "bogus command")
            ]
        };

        int previousEntryIndex = 1;
        int previousEntryTerm = 1; // Term is smaller than the inconsistent log

        // ACT
        await follower.RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, previousEntryIndex, previousEntryTerm, []);

        // ASSERT
        await leader.Received().RespondAppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), false);
        Assert.Single(follower.Entries);
    }

    [Fact]
    // Test 15f
    public async Task WhenFollowerRejectsAppendEntries_LeaderDecrementsNextIndex_ThenTriesAppendEntriesAgain()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        follower.Entries.Returns([new Entry(0, "a")]);

        var follower2 = Substitute.For<INode>();
        follower2.Id.Returns(2);

        Node leader = new([follower, follower2])
        {
            Id = 0,
            Entries = [new Entry(0, "a"), new Entry(1, "b")]
        };

        leader.BecomeLeader();
        await leader.RespondAppendEntries(follower.Id, follower.Term, follower.Entries.Count, false);

        Assert.Equal(1, leader.NextIndexes[follower.Id]);
    }

    [Fact]
    // Test 16
    public void WhenLeaderSendsHeartbeatWithLog_DoesNotReceiveMajority_LogRemainsUncommitted()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        var node2 = Substitute.For<INode>();
        node2.Id.Returns(2);
        Node leader = new([follower, node2])
        {
            Id = 0,
            Entries = [new Entry(1, "command")],
        };

        leader.Heartbeat();

        Assert.Equal(0, leader.CommittedLogIndex);
    }

    [Fact]
    // Test 17
    public async Task WhenLeader_IfNoResponseFromFollower_LeaderContinuesToSendLogEntriesInHeartbeats()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        Node leader = new([follower])
        {
            Id = 0,
            Entries = [new Entry(1, "command")],
        };

        leader.BecomeLeader();
        await leader.Heartbeat();

        await follower.Received(2).RequestAppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Do<List<Entry>>(Assert.NotEmpty));
    }

    [Fact]
    // Test 18
    public async Task IfLeaderCannotCommitAnEntry_ItDoesNotSendResponseToClient()
    {
        var client = Substitute.For<IClient>();
        client.Id.Returns(0);

        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        follower.Entries.Returns([new Entry(0, "a")]);

        var follower2 = Substitute.For<INode>();
        follower2.Id.Returns(2);

        Node leader = new([follower, follower2]) { Id = 0 };

        leader.BecomeLeader();
        leader.ReceiveCommand(client, "a");
        await leader.RespondAppendEntries(follower.Id, follower.Term, follower.Entries.Count, false);

        await client.Received(0).ReceiveLeaderCommitResponse(Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    // Test 19
    public async Task IfNodeRecievesAppendEntries_WithFutureLogs_ThenReject()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);

        Node follower = new([leader]) { Id = 0 };

        await follower.RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 10, 10, [new Entry(10, "a")]);

        await leader.Received().RespondAppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), false);
    }

    [Fact]
    // Test 20
    public async Task NodeReceivesAppendEntries_WithTermAndIndexThatDoNoMatch_RejectUntilFindMatchingLog()
    {
        var follower = Substitute.For<INode>();
        Node leader = new([follower])
        {
            Id = 0,
            Entries = [
                new Entry(1, "a"),
                new Entry(1, "b"),
                new Entry(1, "c"),

            ]
        };

        follower.Id.Returns(1);
        follower.Entries.Returns([
            new Entry(1, "a"),
            new Entry(10, "b"),
            new Entry(10, "c"),
        ]);

        follower.RequestAppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>())
            .Returns(
                async x =>
                {
                    await leader.RespondAppendEntries(follower.Id, follower.Term, follower.Entries.Count, false);
                    await follower.Received().RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 3, 1, []);
                },
                async x =>
                {
                    await leader.RespondAppendEntries(follower.Id, follower.Term, follower.Entries.Count, false);
                    await follower.Received().RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 2, 1, [leader.Entries[2]]);
                },
                async x =>
                {
                    await leader.RespondAppendEntries(follower.Id, follower.Term, follower.Entries.Count, true);
                    await follower.Received().RequestAppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, 1, 1, [leader.Entries[1], leader.Entries[2]]);
                }
            );

        await leader.Heartbeat();
        await leader.Heartbeat();
        await leader.Heartbeat();
    }

    [Fact]
    public void IntegrationTest()
    {
        var node1 = Substitute.For<INode>();
        Node leader = new([node1]) { Id = 0 };

        leader.BecomeCandidate();
    }
}