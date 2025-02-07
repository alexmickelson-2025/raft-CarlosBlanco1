public record ResponseAppendEntriesDTO {
    public long senderId {get; set;} 
    public bool isResponseRejecting {get; set;}
    public int? senderTerm {get; set;}
    public int? commitIndex {get; set;}
    public int? ackedLogIndex {get; set;}
    public ResponseAppendEntriesDTO(long _senderId, bool _isResponseRejecting, int? _senderTern, int? _commitIndex, int? _ackedLogIndex)
    {
        senderId = _senderId;
        isResponseRejecting = _isResponseRejecting;
        senderTerm = _senderTern;
        commitIndex = _commitIndex;
        ackedLogIndex = _ackedLogIndex;
    }
}
