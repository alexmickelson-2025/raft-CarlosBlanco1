public record RequestVoteDTO {
    public long senderId {get; set;} 
    public int senderTerm {get; set;}

    public RequestVoteDTO(long senderId, int senderTerm)
    {
        this.senderId = senderId;
        this.senderTerm = senderTerm;
    }
    
}
