namespace raft_garioncox.Records;
public record RespondEntriesDTO(int FollowerId, int FollowerTerm, int FollowerEntryIndex, bool Response);