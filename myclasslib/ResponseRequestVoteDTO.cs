public record ResponseRequestVoteDTO {
    public long serverNodeId {get; set;}
    public int serverNodeTerm {get; set;}
    public bool wasVoteGiven {get; set;}

    public ResponseRequestVoteDTO(long serverNodeId, int serverNodeTerm, bool wasVoteGiven)
    {
        this.serverNodeId = serverNodeId;
        this.serverNodeTerm = serverNodeTerm;
        this.wasVoteGiven = wasVoteGiven;
    }
}