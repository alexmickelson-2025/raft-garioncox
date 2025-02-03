using System.Data;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.Core.Arguments;
using raft_garioncox;
using raft_garioncox.Records;
using WebSimulation.Components;

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

        Node leader = new([follower, node2])
        {
            Id = 0,
            NextIndexes = new Dictionary<int, int>() { { follower.Id, 0 } }
        };

        string command = DateTime.MaxValue.ToString();

        // ACT
        leader.ReceiveCommand(new ClientCommandDTO(client, command));
        leader.Heartbeat();

        // ASSERT
        follower.Received().RequestAppendEntries(Arg.Any<AppendEntriesDTO>());
    }

    [Fact]
    // Test 2
    public void WhenLeaderReceivesCommandFromClient_ItAppendsEntryToLog()
    {
        var client = Substitute.For<IClient>();
        client.Id.Returns(0);

        string command = DateTime.MaxValue.ToString();
        Node leader = new();

        leader.ReceiveCommand(new ClientCommandDTO(client, command));

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
        follower.Id.Returns(1);
        Node leader = new([follower])
        {
            Entries = [e],
            CommittedLogIndex = 1,
            Id = 0,
            NextIndexes = new Dictionary<int, int>() { { follower.Id, 0 } }
        };

        leader.Heartbeat();

        follower.Received(1).RequestAppendEntries(Arg.Any<AppendEntriesDTO>());
    }

    [Fact]
    // Test 7
    public async Task WhenFollowerLearnsLogIsCommitted_ItCommitsThatLog()
    {
        var mockNode = Substitute.For<INode>();
        int mockTerm = 0;
        int mockLogIndex = 0;
        Node follower = new([mockNode]) { Id = 0 };

        await follower.RequestAppendEntries(new AppendEntriesDTO(mockNode.Id, mockTerm, mockLogIndex, 0, 0, []));

        Assert.Equal(mockLogIndex, follower.CommittedLogIndex);
    }

    [Fact]
    // Test 8
    public async Task WhenLeaderReceivesMajorityResponsesForLog_ItCommitsIt()
    {
        var mockClient = Substitute.For<IClient>();
        mockClient.Id.Returns(0);

        var mockfollower = Substitute.For<INode>();
        mockfollower.Id.Returns(1);
        int mockTerm = 0;
        int mockLogIndex = 1;

        var mockNode2 = Substitute.For<INode>();
        mockNode2.Id.Returns(2);

        Node leader = new([mockfollower, mockNode2]) { Id = 0 };

        await leader.ReceiveCommand(new ClientCommandDTO(mockClient, "a"));

        await leader.RespondAppendEntries(new RespondEntriesDTO(mockfollower.Id, mockTerm, mockLogIndex, true));

        Assert.Equal(1, leader.CommittedLogIndex);
    }

    [Fact]
    // Test 9
    public void LeaderCommitsLogs_ByIncrementingCommittedLogIndex()
    {
        var mockClient = Substitute.For<IClient>();
        mockClient.Id.Returns(0);

        Node leader = new() { Id = 0 };

        leader.ReceiveCommand(new ClientCommandDTO(mockClient, "a"));
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
        int mockTerm = 0;
        int mockLogIndex = 0;
        Node follower = new([leader]) { Id = 0 };

        await follower.RequestAppendEntries(new AppendEntriesDTO(leader.Id, mockTerm, mockLogIndex, 0, 0, [e]));

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
        int mockTerm = 0;
        int mockLogIndex = 0;
        Node follower = new([leader]) { Id = 0 };

        await follower.RequestAppendEntries(new AppendEntriesDTO(leader.Id, mockTerm, mockLogIndex, 0, 0, []));

        await leader.Received(1).RespondAppendEntries(Arg.Is<RespondEntriesDTO>(dto =>
            dto.FollowerId == follower.Id &&
            dto.FollowerTerm == follower.Term &&
            dto.FollowerEntryIndex == follower.CommittedLogIndex
        ));
    }

    [Fact]
    // Test 12
    public async Task WhenLeaderReceivesMajorityResponseForAppendEntries_ItSendsConfirmationToClient()
    {
        var client = Substitute.For<IClient>();
        client.Id.Returns(0);

        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        Entry[] mockEntries = new Entry[1];
        int mockTerm = 0;

        var follower2 = Substitute.For<INode>();
        follower2.Id.Returns(2);

        Node leader = new([follower, follower2]) { Id = 0 };

        await leader.ReceiveCommand(new ClientCommandDTO(client, "a"));
        await leader.RespondAppendEntries(new RespondEntriesDTO(follower.Id, mockTerm, mockEntries.Length, true));

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

        leader.ReceiveCommand(new ClientCommandDTO(mockClient, entry));
        leader.CommitEntry();

        Assert.Equal(entry, leader.LogState);
    }

    [Fact]
    // Test 14
    public async Task WhenFollowerReceivesHeartbeat_ItMatchesCommitIndexOfHeartbeat()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        int mockTerm = 0;
        int mockCommittedLogIndex = 3;
        Node follower = new([leader]) { Id = 0 };

        await follower.RequestAppendEntries(new AppendEntriesDTO(leader.Id, mockTerm, mockCommittedLogIndex, 0, 0, []));

        Assert.Equal(mockCommittedLogIndex, follower.CommittedLogIndex);
    }

    [Fact]
    // Test 15a
    public void LeaderIncludesIndexAndTerm_OfEntryPrecedingNewEntry_WhenSendingAppendEntries()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        Node leader = new([follower])
        {
            Id = 0,
            NextIndexes = new Dictionary<int, int>() { { follower.Id, 0 } }
        };

        leader.Heartbeat();

        follower.Received().RequestAppendEntries(Arg.Is<AppendEntriesDTO>(dto =>
            dto.PreviousEntryIndex == 0 && dto.PreviousEntryTerm == 0
        ));
    }

    [Fact]
    // Test 15b
    public async Task WhenFollowerReceivesAppendEntries_AndDoesNotFindIndexAndTermMatch_ItRefusesNewEntries()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        int mockTerm = 0;
        int mockCommittedLogIndex = 0;
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

        await follower.RequestAppendEntries(new AppendEntriesDTO(leader.Id, mockTerm, mockCommittedLogIndex, previousEntryIndex, previousEntryTerm, newEntries));

        await leader.Received().RespondAppendEntries(Arg.Is<RespondEntriesDTO>(dto => dto.Response == false));
        Assert.Equal(2, follower.Entries.Count);
    }

    [Fact]
    // Test 15c
    public async Task WhenFollowerReceivesAppendEntries_AndFindsEntryInItsLogWithNewerTermInParams_ItAcceptsNewEntries()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        int mockTerm = 0;
        int mockCommittedLogIndex = 0;
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

        await follower.RequestAppendEntries(new AppendEntriesDTO(leader.Id, mockTerm, mockCommittedLogIndex, previousEntryIndex, previousEntryTerm, newEntries));

        await leader.Received().RespondAppendEntries(Arg.Is<RespondEntriesDTO>(dto => dto.Response == true));
        Assert.Equal(newEntries[0].Value, follower.Entries[2].Value);
        Assert.Equal(newEntries[1].Value, follower.Entries[3].Value);
    }

    [Fact]
    // Test 15d
    public async Task WhenFollowerReceivesAppendEntries_WhenLeaderPreviousLogIndexGreaterThanCurrentIndex_LeaderDecreasesPreviousTermEntry()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        int mockTerm = 0;
        Entry[] mockEntries = [new Entry(0, "a")];

        var follower2 = Substitute.For<INode>();
        follower2.Id.Returns(2);

        Node leader = new([follower, follower2])
        {
            Id = 0,
            Entries = [new Entry(0, "a"), new Entry(1, "b")]
        };

        leader.BecomeLeader();
        Assert.Equal(2, leader.NextIndexes[follower.Id]);

        await leader.RespondAppendEntries(new RespondEntriesDTO(follower.Id, mockTerm, mockEntries.Length, false));
        Assert.Equal(1, leader.NextIndexes[follower.Id]);
    }

    [Fact]
    // Test 15e
    public async Task WhenFollowerReceivesAppendEntries_WhenPreviousTermLessThanStoredTerm_ItDeletesAllEntriesAfterInconsistentLog()
    {
        // ARRANGE
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        int mockTerm = 0;
        int mockCommittedLogIndex = 0;

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
        await follower.RequestAppendEntries(new AppendEntriesDTO(leader.Id, mockTerm, mockCommittedLogIndex, previousEntryIndex, previousEntryTerm, []));

        // ASSERT
        await leader.Received().RespondAppendEntries(Arg.Is<RespondEntriesDTO>(dto => dto.Response == false));
        Assert.Single(follower.Entries);
    }

    [Fact]
    // Test 15f
    public async Task WhenFollowerRejectsAppendEntries_LeaderDecrementsNextIndex_ThenTriesAppendEntriesAgain()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        int mockTerm = 0;
        Entry[] mockEntries = [new Entry(0, "a")];

        var follower2 = Substitute.For<INode>();
        follower2.Id.Returns(2);

        Node leader = new([follower, follower2])
        {
            Id = 0,
            Entries = [new Entry(0, "a"), new Entry(1, "b")]
        };

        leader.BecomeLeader();
        await leader.RespondAppendEntries(new RespondEntriesDTO(follower.Id, mockTerm, mockEntries.Length, false));

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
            Entries = [new Entry(1, "command"), new Entry(1, "command2")],
            NextIndexes = new Dictionary<int, int>() { { follower.Id, 0 } }
        };

        await leader.Heartbeat();
        await leader.Heartbeat();

        await follower.Received(2).RequestAppendEntries(Arg.Is<AppendEntriesDTO>(dto => dto.Entries.Count > 0));
    }

    [Fact]
    // Test 18
    public async Task IfLeaderCannotCommitAnEntry_ItDoesNotSendResponseToClient()
    {
        var client = Substitute.For<IClient>();
        client.Id.Returns(0);

        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        int mockTerm = 0;
        Entry[] mockEntries = [new Entry(0, "a")];

        var follower2 = Substitute.For<INode>();
        follower2.Id.Returns(2);

        Node leader = new([follower, follower2]) { Id = 0 };

        leader.BecomeLeader();
        await leader.ReceiveCommand(new ClientCommandDTO(client, "a"));
        await leader.RespondAppendEntries(new RespondEntriesDTO(follower.Id, mockTerm, mockEntries.Length, false));

        await client.Received(0).ReceiveLeaderCommitResponse(Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    // Test 19
    public async Task IfNodeRecievesAppendEntries_WithFutureLogs_ThenReject()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(1);
        int mockTerm = 0;
        int mockCommittedLogIndex = 0;

        Node follower = new([leader]) { Id = 0 };

        await follower.RequestAppendEntries(new AppendEntriesDTO(leader.Id, mockTerm, mockCommittedLogIndex, 10, 10, [new Entry(10, "a")]));

        await leader.Received().RespondAppendEntries(Arg.Is<RespondEntriesDTO>(dto => dto.Response == false));
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
        Entry[] mockEntries = [
            new Entry(1, "a"),
            new Entry(10, "b"),
            new Entry(10, "c"),
        ];
        int mockTerm = 0;

        follower.RequestAppendEntries(Arg.Any<AppendEntriesDTO>())
            .Returns(
                async x =>
                {
                    await leader.RespondAppendEntries(new RespondEntriesDTO(follower.Id, mockTerm, mockEntries.Length, false));
                    await follower.Received().RequestAppendEntries(Arg.Is<AppendEntriesDTO>(dto =>
                        dto.LeaderId == leader.Id &&
                        dto.LeaderTerm == leader.Term &&
                        dto.CommittedLogIndex == leader.CommittedLogIndex &&
                        dto.PreviousEntryIndex == 3 &&
                        dto.PreviousEntryTerm == 1 &&
                        dto.Entries.Count == 0)
                    );
                },
                async x =>
                {
                    await leader.RespondAppendEntries(new RespondEntriesDTO(follower.Id, mockTerm, mockEntries.Length, false));
                    await follower.Received().RequestAppendEntries(Arg.Is<AppendEntriesDTO>(dto =>
                        dto.LeaderId == leader.Id &&
                        dto.LeaderTerm == leader.Term &&
                        dto.CommittedLogIndex == leader.CommittedLogIndex &&
                        dto.PreviousEntryIndex == 2 &&
                        dto.PreviousEntryTerm == 1 &&
                        dto.Entries[0] == leader.Entries[2])
                    );
                },
                async x =>
                {
                    await leader.RespondAppendEntries(new RespondEntriesDTO(follower.Id, mockTerm, mockEntries.Length, true));
                    await follower.Received().RequestAppendEntries(Arg.Is<AppendEntriesDTO>(dto =>
                        dto.LeaderId == leader.Id &&
                        dto.LeaderTerm == leader.Term &&
                        dto.CommittedLogIndex == leader.CommittedLogIndex &&
                        dto.PreviousEntryIndex == 1 &&
                        dto.PreviousEntryTerm == 1 &&
                        dto.Entries[0] == leader.Entries[1] &&
                        dto.Entries[1] == leader.Entries[2])
                    );
                }
            );

        await leader.Heartbeat();
        await leader.Heartbeat();
        await leader.Heartbeat();
    }

    [Fact]
    public async Task GivenLeaderHasNewLogs_OnNextHeartbeat_ItSendsThem()
    {
        var client = Substitute.For<IClient>();
        client.Id.Returns(0);
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        Node leader = new([follower])
        {
            Id = 1,
            Term = 1,
            Entries = [new Entry(0, "a"), new Entry(0, "b"), new Entry(1, "c")],
            NextIndexes = new Dictionary<int, int>() { { follower.Id, 3 } }
        };

        await leader.ReceiveCommand(new ClientCommandDTO(client, "d"));
        Assert.Equal(4, leader.Entries.Count);

        await leader.Heartbeat();
        await follower.Received().RequestAppendEntries(Arg.Is<AppendEntriesDTO>(dto =>
                dto.LeaderId == leader.Id &&
                dto.LeaderTerm == leader.Term &&
                dto.CommittedLogIndex == leader.CommittedLogIndex &&
                dto.PreviousEntryIndex == 2 &&
                dto.PreviousEntryTerm == leader.Entries[2].Term
            ));
    }

    [Fact]
    public async Task GivenFollowerHasNoLogs_WhenItReceivesAppendEntriesWithNewLogs_ItAppendsThem()
    {
        var leader = Substitute.For<INode>();
        leader.Id.Returns(0);
        int mockTerm = 0;
        Node follower = new([leader]) { Id = 1 };

        await follower.RequestAppendEntries(new AppendEntriesDTO(leader.Id, mockTerm, 0, 0, 0, [new Entry(0, "a")]));

        Assert.NotEmpty(follower.Entries);
    }

    [Fact]
    public async Task GivenFollowerHasLogs_WhenItReceivesAppendEntriesWithNewLogs_ItAppendsThem()
    {
        List<Entry> entries = [
            new Entry(0, "a"),
            new Entry(0, "b"),
            new Entry(1, "c"),
            new Entry(1, "d"),
        ];

        var leader = Substitute.For<INode>();
        leader.Id.Returns(0);
        int mockTerm = 0;

        Node follower = new([leader])
        {
            Id = 1,
            Entries = [entries[0], entries[1], entries[2]]
        };

        await follower.RequestAppendEntries(new AppendEntriesDTO(leader.Id, mockTerm, 0, 0, 0, [entries[3]]));

        Assert.Equal(4, follower.Entries.Count);
    }
}