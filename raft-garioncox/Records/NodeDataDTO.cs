namespace raft_garioncox.Records;
public record NodeDataDTO(
     int Id,
      string LogState,
      int ElectionTimeout,
      int Term,
      int? CurrentTermLeader,
      int CommittedLogIndex,
      NODESTATE State,
      int IntervalScalar
);