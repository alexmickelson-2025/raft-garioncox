using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NSubstitute;
using raft_garioncox;

namespace RaftSimulationTests;

public class SimTests
{
    [Fact]
    // Testing 1
    public void WhenLeaderActive_SendsHeartbeatWithin50ms()
    {
        // ARRANGE
        var follower = Substitute.For<INode>();
        Node leader = new(0)
        {
            State = NODESTATE.LEADER,
            Neighbors = new Dictionary<int, INode>
            {
                { 1, follower },
            },
            ElectionTimeout = 50
        };

        // ACT
        Thread t = leader.Run();

        Thread.Sleep(150);
        leader.Stop();
        t.Join();

        // ASSERT
        follower.Received().AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex);
    }

    [Fact]
    public async Task WhenLeaderActive_SendsHeartBeatEvery50ms()
    {
        // ARRANGE
        var follower = Substitute.For<INode>();
        Node leader = new(0)
        {
            State = NODESTATE.LEADER,
            Neighbors = new Dictionary<int, INode>
            {
                { 1, follower },
            },
            ElectionTimeout = 50
        };

        // ACT
        Thread t = leader.Run();

        Thread.Sleep(150);
        leader.Stop();
        t.Join();

        // ASSERT
        await follower.Received().AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex, null);
    }

    [Fact]
    // Testing 2
    public async Task Cluster_WhenNodeReceivesAppendEntries_ThenRemembersOtherNodeIsCurrentLeader()
    {
        var leader = Substitute.For<INode>();
        leader.State.Returns(NODESTATE.LEADER);
        leader.Id.Returns(1);
        Node follower = new(1)
        {
            Neighbors = new Dictionary<int, INode> { { leader.Id, leader }, }
        };

        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex);

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
    public void WhenFollowerDoesntGetMessageDuringTimeout_ItStartsAnElection()
    {
        Node n1 = new(0);
        var n2 = Substitute.For<INode>();
        var n3 = Substitute.For<INode>();
        n1.ElectionTimeout = 80;
        n1.Neighbors = new Dictionary<int, INode>
            {
                { 1, n2 },
                { 2, n3 }
            };

        // ACT
        Thread t = n1.Run();

        Thread.Sleep(150);
        n1.Stop();
        t.Join();

        // ASSERT
        Assert.Equal(1, n1.Term);
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
        Node n1 = new(0)
        {
            ElectionTimeout = 10
        };

        n1.ResetElectionTimeout();

        Assert.InRange(n1.ElectionTimeout, 150, 300);
    }

    [Fact]
    // Testing 6
    public void WhenNewElectionBegins_TermIsIncrementedBy1()
    {
        Node n = new(0)
        {
            ElectionTimeout = 10
        };
        int previousTerm = n.Term;

        Thread t = n.Run();

        Thread.Sleep(60);
        n.Stop();
        t.Join();

        int newTerm = n.Term;

        Assert.True(newTerm > previousTerm);
    }

    [Fact]
    // Testing 7
    public async Task WhenFollowerGetsAppendEntriesMessage_ItResetsElectionTimer()
    {
        // ARRANGE
        var leader = Substitute.For<INode>();
        leader.Id.Returns(0);
        var n2 = Substitute.For<INode>();
        n2.Id.Returns(1);
        Node follower = new(0)
        {
            Neighbors = new Dictionary<int, INode>()
            {
                { leader.Id, leader },
                { n2.Id, n2 },
            }
        };


        // ACT
        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex);

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
        Assert.Equal(n.Id, n.CurrentLeader);
    }

    [Fact]
    // Testing 8
    public void Cluster_WhenOneBecomesCandidate_ShouldBecomeLeader()
    {
        var n2 = Substitute.For<INode>();
        var n3 = Substitute.For<INode>();
        Node n1 = new(0)
        {
            Neighbors = new Dictionary<int, INode>()
            {
                { 1, n2 },
                { 2, n3 },
            }
        };

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
        var candidate = Substitute.For<INode>();
        candidate.Id.Returns(1);
        candidate.Term.Returns(0);
        Node follower = new(0)
        {
            Neighbors = new Dictionary<int, INode>()
            {
                { candidate.Id, candidate },
            }
        };

        follower.RequestVoteFor(candidate.Id, candidate.Term);

        Assert.Equal(candidate.Id, follower.Vote);
        Assert.True(follower.HasVoted);
    }

    [Fact]
    // Testing 9
    public void CandidateReceivesMajorityVotes_WhileWaitinForUnresponsiveNode_StillBecomesLeader()
    {
        Node candidate = new(0)
        {
            ElectionTimeout = 10
        };
        var n1 = Substitute.For<INode>();
        var n2 = Substitute.For<INode>();

        n2.When(n => n.RequestVoteFor(Arg.Any<int>(), Arg.Any<int>()))
            .Do(x => candidate.ReceiveVote(true));


        Thread t = candidate.Run();

        Thread.Sleep(50);

        candidate.Stop();
        t.Join();

        Assert.Equal(NODESTATE.LEADER, candidate.State);
    }

    [Fact]
    // Testing 10
    public void GivenAFollowerHasNotVoted_WhenARequestRPCIsSentWithALaterTerm_ThenItRespondsYes()
    {
        var candidate = Substitute.For<INode>();
        candidate.Id = 1;
        candidate.Term = 1;

        Node follower = new(0)
        {
            Term = 0,
            HasVoted = false,
            Neighbors = new Dictionary<int, INode>()
            {
                { candidate.Id, candidate },
            }
        };

        follower.RequestVoteForRPC(candidate.Id, candidate.Term);

        candidate.Received().ReceiveVote(true);
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
    public async Task WhenCandidateReceivesMessageFromLaterTerm_BecomesFollower()
    {
        var n2 = Substitute.For<INode>();
        n2.Term.Returns(1);
        n2.Id.Returns(1);
        n2.CommittedLogIndex.Returns(0);
        Node n1 = new(0)
        {
            Term = 0,
            State = NODESTATE.CANDIDATE,
            Neighbors = new Dictionary<int, INode>()
            {
                {n2.Id, n2}
            }
        };

        await n1.AppendEntries(n2.Id, n2.Term, n2.CommittedLogIndex);

        Assert.Equal(NODESTATE.FOLLOWER, n1.State);
    }

    [Fact]
    // Testing 13
    public async Task WhenCandidateReceivesMessageFromEqualTerm_BecomesFollower()
    {
        var n2 = Substitute.For<INode>();
        n2.Term.Returns(1);
        n2.Id.Returns(1);
        n2.CommittedLogIndex.Returns(0);

        Node n1 = new(0)
        {
            Term = 0,
            State = NODESTATE.CANDIDATE,
            Neighbors = new Dictionary<int, INode>()
            {
                {n2.Id, n2}
            }
        };

        await n1.AppendEntries(n2.Id, n2.Term, n2.CommittedLogIndex);

        Assert.Equal(NODESTATE.FOLLOWER, n1.State);
    }

    [Fact]
    // Testing 14
    public void IfNodeReceivesSecondVoteRequest_ShouldRespondNo()
    {
        Node n1 = new(0);
        var n2 = Substitute.For<INode>();
        n2.Term.Returns(0);
        n2.Id.Returns(1);
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
        var n2 = Substitute.For<INode>();
        n2.Term.Returns(0);
        n2.Id.Returns(1);
        n1.Term = 0;
        n1.RequestVoteFor(n2.Id, n2.Term);

        n2.Term = 1;
        bool actual = n1.RequestVoteFor(n2.Id, n2.Term);

        Assert.True(actual);
    }

    [Fact]
    // Testing 16
    public void GivenCandidate_WhenElectionTimerExpiresInsideElection_NewElectionStarted()
    {
        var n1 = Substitute.For<INode>();
        var n2 = Substitute.For<INode>();
        Node candidate = new(0)
        {
            Term = 1,
            State = NODESTATE.CANDIDATE,
            Neighbors = new Dictionary<int, INode>()
                {
                    {1, n1},
                    {2, n2}
                },
            ElectionTimeout = 10
        };

        Thread t = candidate.Run();

        Thread.Sleep(350);
        candidate.Stop();

        t.Join();

        Assert.True(candidate.Term > 1);
    }

    [Fact]
    // Testing 17
    public async Task WhenFollowerReceivesAppendEntriesRequest_ItSendsResponse()
    {
        INode mockNode = Substitute.For<INode>();
        mockNode.Id.Returns(1);
        mockNode.Term.Returns(0);
        mockNode.CommittedLogIndex.Returns(0);
        Node n1 = new(0)
        {
            Neighbors = new() { { mockNode.Id, mockNode } }
        };

        await n1.AppendEntries(mockNode.Id, mockNode.Term, mockNode.CommittedLogIndex);
        await mockNode.Received().ReceiveAppendEntriesResponse(n1.Term, n1.CommittedLogIndex, Arg.Any<bool>());
    }

    [Fact]
    // Testing 18
    public async Task WhenFollowerReceivesAppendEntriesRequest_WithPreviousTerm_ItRejects()
    {
        INode mockLeader = Substitute.For<INode>();
        mockLeader.Term = 0;
        mockLeader.CommittedLogIndex = 0;
        Node n1 = new(0)
        {
            Term = 1,
            Neighbors = new() { { mockLeader.Id, mockLeader } }
        };

        await n1.AppendEntries(mockLeader.Id, mockLeader.Term, mockLeader.CommittedLogIndex);

        await mockLeader.Received().ReceiveAppendEntriesResponse(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>());
    }

    [Fact]
    // Test 19
    public void WhenCandidateWinsElection_ItImmediatelySendsHeartbeat()
    {
        var n1 = Substitute.For<INode>();
        Node candidate = new(0)
        {
            ElectionTimeout = 10,
            Neighbors = new Dictionary<int, INode>()
                {
                    {1, n1}
                }
        };

        n1.When(n => n.RequestVoteForRPC(Arg.Any<int>(), Arg.Any<int>()))
            .Do(n => candidate.ReceiveVote(true));

        candidate.BecomeLeader();

        n1.Received().AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task WhenFollowerReceivesHeartbeatFromLeader_ItUpdatesItsTermToMatchLeader()
    {
        Node follower = new(0);
        var leader = Substitute.For<INode>();
        leader.Term.Returns(2);
        leader.Id.Returns(1);
        leader.CommittedLogIndex.Returns(0);

        follower.CurrentLeader = 1;
        follower.Neighbors = new Dictionary<int, INode>()
            {
                {leader.Id, leader}
            };

        await follower.AppendEntries(leader.Id, leader.Term, leader.CommittedLogIndex);

        Assert.Equal(leader.Term, follower.Term);
    }
}