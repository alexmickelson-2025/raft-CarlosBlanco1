public record ResponseAppendEntriesDTO
{
    public long senderId { get; set; } 
    public bool isResponseRejecting { get; set; }
    public int? senderTerm { get; set; }
    public int? commitIndex { get; set; }
    public int? ackedLogIndex { get; set; }
    public ResponseAppendEntriesDTO(long senderId, bool isResponseRejecting, int? senderTerm, int? commitIndex, int? ackedLogIndex)
    {
        this.senderId = senderId;
        this.isResponseRejecting = isResponseRejecting;
        this.senderTerm = senderTerm;
        this.commitIndex = commitIndex;
        this.ackedLogIndex = ackedLogIndex;
    }
}
