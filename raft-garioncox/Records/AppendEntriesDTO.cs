namespace raft_garioncox.Records;
public record AppendEntriesDTO(int LeaderId, int LeaderTerm, int CommittedLogIndex, int PreviousEntryIndex, int PreviousEntryTerm, List<Entry> Entries);