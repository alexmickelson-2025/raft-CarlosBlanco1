public record ResponseRequestVoteDTO {
    public long serverNodeId {get; set;}
    public int serverNodeTerm {get; set;}
    public bool wasVoteGiven {get; set;}
}