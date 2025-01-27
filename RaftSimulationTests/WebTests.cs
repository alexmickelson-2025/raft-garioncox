using System.Security.Cryptography.X509Certificates;
using NSubstitute;
using raft_garioncox;

namespace RaftSimulationTests;

public class WebTests
{
    [Fact]
    public async Task WhenLeaderIsPaused_OtherNodesGetNoHeartbeatFor400ms()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        Node leader = new(0)
        {
            State = NODESTATE.LEADER,
            Neighbors = new Dictionary<int, INode>() {
                {follower.Id, follower}
            }
        };

        Thread t = leader.Run();
        leader.Pause();

        Thread.Sleep(400);
        await follower.Received(0).AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());

        Thread.Sleep(400);

        leader.Stop();
        t.Join();
        await follower.Received(0).AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task WhenLeaderIsPaused_WhenOtherNodesGetNoHeartbeatFor400ms_TheyGetUnpausedAndHeartbeatsResume()
    {
        var follower = Substitute.For<INode>();
        follower.Id.Returns(1);
        Node leader = new(0)
        {
            State = NODESTATE.LEADER,
            Neighbors = new Dictionary<int, INode>() {
                {follower.Id, follower}
            }
        };

        Thread t = leader.Run();
        leader.Pause();

        Thread.Sleep(400);
        await follower.Received(0).AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
        leader.Unpause();

        Thread.Sleep(400);
        await follower.Received().AppendEntries(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());

        leader.Stop();
        t.Join();
    }
}