using System.Data.Common;

namespace raft_garioncox.Records;

public record NodeDataDTO
{
      public int Id { get; init; }
      public string LogState { get; init; }
      public int ElectionTimeout { get; init; }
      public int Term { get; init; }
      public int? CurrentTermLeader { get; init; }
      public int CommittedLogIndex { get; init; }
      public NODESTATE State { get; init; }
      public int IntervalScalar { get; init; }
      public List<Entry> Entries { get; init; }
      public bool IsPaused { get; init; }

      public NodeDataDTO(int id, string logState, int electionTimeout, int term, int? currentTermLeader, int committedLogIndex, NODESTATE state, int intervalScalar, List<Entry> entries, bool isPaused)
      {
            Id = id;
            LogState = logState;
            ElectionTimeout = electionTimeout;
            Term = term;
            CurrentTermLeader = currentTermLeader;
            CommittedLogIndex = committedLogIndex;
            State = state;
            IntervalScalar = intervalScalar;
            Entries = entries;
            IsPaused = isPaused;
      }
}
