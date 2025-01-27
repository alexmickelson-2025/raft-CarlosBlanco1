namespace myclasslib;
public interface IServerNode
{
    public long NodeId {get; set;}
    public long LeaderNodeId {get; set;}
    public ServerState State { get; set; }
    public int CurrentTerm {get; set;}
    public int CommitIndex {get; set;}
    public System.Timers.Timer? ElectionTimer {get; set;}
    public DateTime ElectionTimerStartedAt {get; set;}
    public System.Timers.Timer? HeartbeatTimer {get; set;}
    public Dictionary<long, IServerNode> IdToNode {get; set;}
    public Dictionary<long, bool?> IdToVotedForMe {get; set;}
    public Dictionary<long, int> IdToNextIndex {get; set;}
    public Dictionary<long, bool?> IdToLogValidationStatus {get; set;}
    public Dictionary<int, string> InternalStateMachine {get; set;}
    public Task AppendEntriesRPC(long senderId, int senderTerm, LogEntry? entry, int? highestCommitedIndex);
    public Task ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting, int? senderTerm, int? commitIndex);
    public Task RequestVoteRPC(long senderId, int senderTerm);
    public Task ResponseRequestVoteRPC(long serverNodeId, bool wasVoteGiven);
    public Task SendHeartBeat();
    public Task SendVotes();
    public void StartNewElection();
    public void StartNewElectionTimer(double electionTimeout = 0);
    public Task TransitionToLeader();
    public Task TransitionToCandidate();
    public Task TransitionToPaused();
    public Task TransitionToFollower();
    public void AddNeighbors(List<IServerNode> neighbors);
    public void SendCommandToLeader(LogEntry entry);
    public void SendConfirmationResponseToClient();
}