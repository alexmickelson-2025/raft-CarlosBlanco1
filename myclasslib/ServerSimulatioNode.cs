using myclasslib;

public class ServerSimulatioNode : IServerNode
{
    public ServerNode _innerServerNode {get; set;}
    public long NodeId { get => ((IServerNode)_innerServerNode).NodeId; set => ((IServerNode)_innerServerNode).NodeId = value; }
    public long LeaderNodeId { get => ((IServerNode)_innerServerNode).LeaderNodeId; set => ((IServerNode)_innerServerNode).LeaderNodeId = value; }
    public ServerState State { get => ((IServerNode)_innerServerNode).State; set => ((IServerNode)_innerServerNode).State = value; }
    public int CurrentTerm { get => ((IServerNode)_innerServerNode).CurrentTerm; set => ((IServerNode)_innerServerNode).CurrentTerm = value; }
    public System.Timers.Timer ElectionTimer { get => ((IServerNode)_innerServerNode).ElectionTimer; set => ((IServerNode)_innerServerNode).ElectionTimer = value; }
    public System.Timers.Timer? HeartbeatTimer { get => ((IServerNode)_innerServerNode).HeartbeatTimer; set => ((IServerNode)_innerServerNode).HeartbeatTimer = value; }
    public Dictionary<long, IServerNode> IdToNode { get => ((IServerNode)_innerServerNode).IdToNode; set => ((IServerNode)_innerServerNode).IdToNode = value; }
    public Dictionary<long, bool?> IdToVotedForMe { get => ((IServerNode)_innerServerNode).IdToVotedForMe; set => ((IServerNode)_innerServerNode).IdToVotedForMe = value; }

    public ServerSimulatioNode(ServerNode innerServerNode)
    {
        _innerServerNode = innerServerNode;
    }

    public void AppendEntriesRPC(long senderId, int senderTerm)
    {
        ((IServerNode)_innerServerNode).AppendEntriesRPC(senderId, senderTerm);
    }

    public string ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting)
    {
        return ((IServerNode)_innerServerNode).ResponseAppendEntriesRPC(senderId, isResponseRejecting);
    }


    public void SendHeartBeat()
    {
        ((IServerNode)_innerServerNode).SendHeartBeat();
    }

    public void StartNewElection()
    {
        ((IServerNode)_innerServerNode).StartNewElection();
    }

    public void TransitionToLeader()
    {
        ((IServerNode)_innerServerNode).TransitionToLeader();
    }

    public void RequestVoteRPC(long senderId, int senderTerm)
    {
        ((IServerNode)_innerServerNode).RequestVoteRPC(senderId, senderTerm);
    }

    public void SendVotes()
    {
        ((IServerNode)_innerServerNode).SendVotes();
    }

    public void TransitionToCandidate()
    {
        ((IServerNode)_innerServerNode).TransitionToCandidate();
    }

    public void ResponseRequestVoteRPC(long serverNodeId, bool wasVoteGiven)
    {
        ((IServerNode)_innerServerNode).ResponseRequestVoteRPC(serverNodeId, wasVoteGiven);
    }

    public void TransitionToPaused()
    {
        ((IServerNode)_innerServerNode).TransitionToPaused();
    }

    public void StartNewElectionTimer(double electionTimeout = 0)
    {
        ((IServerNode)_innerServerNode).StartNewElectionTimer(electionTimeout);
    }

    public void AddNeighbors(List<IServerNode> neighbors)
    {
        ((IServerNode)_innerServerNode).AddNeighbors(neighbors);
    }
}