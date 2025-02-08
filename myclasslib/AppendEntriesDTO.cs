using myclasslib;

public record AppendEntriesDTO
{
    public long senderId { get; set; }
    public int senderTerm { get; set; }
    public int highestCommittedIndex { get; set; }
    public List<LogEntry>? newEntries { get; set; }
    public int? prevLogIndex { get; set; }
    public int? prevLogTerm { get; set; }

    public AppendEntriesDTO(long senderId, int senderTerm, int highestCommittedIndex, 
                            List<LogEntry>? newEntries = null, int? prevLogIndex = null, int? prevLogTerm = null)
    {
        this.senderId = senderId;
        this.senderTerm = senderTerm;
        this.highestCommittedIndex = highestCommittedIndex;
        this.newEntries = newEntries;
        this.prevLogIndex = prevLogIndex;
        this.prevLogTerm = prevLogTerm;
    }
}
