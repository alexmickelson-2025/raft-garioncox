using System.Diagnostics;
using System.Security.AccessControl;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NSubstitute.Core.Arguments;
using raft_garioncox;

namespace RaftSimulationTests;

public class ReplciationTests
{
    [Fact]
    // Testing 1
    public void WhenLeaderReceivesCommandRequest_ItAppendsAnEntryToItsLog()
    {
        throw new NotImplementedException();
    }
}