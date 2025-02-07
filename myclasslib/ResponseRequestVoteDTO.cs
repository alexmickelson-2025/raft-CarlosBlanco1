public record ResponseRequestVoteDTO {
    public long serverNodeId {get; set;}
    public bool wasVoteGiven {get; set;}
}