using myclasslib;

public record AppendEntriesDTO
{
    public long senderId;
    public int senderTerm; 
    public int highestCommittedIndex;
    public List<LogEntry>? newEntries; 
    public int? prevLogIndex; 
    public int? prevLogTerm;
    public AppendEntriesDTO(long _senderId, int _senderTerm,int _highestCommitedIndex, List<LogEntry>? _entries = null, int? _prevLogIndex = null, int? _prevLogTerm = null)
    {
        senderId = _senderId;
        senderTerm = _senderTerm;
        newEntries = _entries;
        prevLogIndex = _prevLogIndex;
        prevLogTerm = _prevLogTerm;
        highestCommittedIndex = _highestCommitedIndex; 
    }
}
