using myclasslib;
public record NodeDataDTO{
    
    public string Id { get; set; }
    public string Status { get; set; }
    public int ElectionTimeout { get; set; }
    public DateTime ElectionStartedAt {get; set;}
    public int Term { get; set; }
    public string? CurrentTermLeader { get; set; }
    public int CommittedEntryIndex { get; set; }
    public List<LogEntry> Logs { get; set; } = new();
    public Dictionary<int, string> InternalStateMachine { get; set; } = new();
    public string State { get; set; }
    public double NodeIntervalScalar { get; set; }

}