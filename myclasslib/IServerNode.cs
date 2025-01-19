namespace myclasslib;
public interface IServerNode
{
    public long NodeId {get; set;}
    public long LeaderNodeId {get; set;}
    public ServerState State { get; set; }
    public int CurrentTerm {get; set;}
    public System.Timers.Timer? ElectionTimer {get; set;}
    public DateTime ElectionTimerStartedAt {get; set;}
    public System.Timers.Timer? HeartbeatTimer {get; set;}
    public Dictionary<long, IServerNode> IdToNode {get; set;}
    public Dictionary<long, bool?> IdToVotedForMe {get; set;}
    public Task AppendEntriesRPC(long senderId, int senderTerm);
    public Task ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting);
    public Task RequestVoteRPC(long senderId, int senderTerm);
    public Task ResponseRequestVoteRPC(long serverNodeId, bool wasVoteGiven);
    public void SendHeartBeat();
    public void SendVotes();
    public void StartNewElection();
    public void StartNewElectionTimer(double electionTimeout = 0);
    public void TransitionToLeader();
    public void TransitionToCandidate();
    public void TransitionToPaused();
    public void TransitionToFollower();
    public void AddNeighbors(List<IServerNode> neighbors);
}
