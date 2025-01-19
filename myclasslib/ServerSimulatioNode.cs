using myclasslib;

public class ServerSimulatioNode : IServerNode
{
    public ServerNode _innerServerNode { get; set; }
    public static int IntervalScaleValue = 12;
    public static int NetworkRequestDelay { get; set; } = 1000;
    public static int NetworkResponseDelay { get; set; } = 0;
    private Timer timer {get; set;}
    public long NodeId { get => ((IServerNode)_innerServerNode).NodeId; set => ((IServerNode)_innerServerNode).NodeId = value; }
    public long LeaderNodeId { get => ((IServerNode)_innerServerNode).LeaderNodeId; set => ((IServerNode)_innerServerNode).LeaderNodeId = value; }
    public ServerState State { get => ((IServerNode)_innerServerNode).State; set => ((IServerNode)_innerServerNode).State = value; }
    public int CurrentTerm { get => ((IServerNode)_innerServerNode).CurrentTerm; set => ((IServerNode)_innerServerNode).CurrentTerm = value; }
    public System.Timers.Timer ElectionTimer { get => ((IServerNode)_innerServerNode).ElectionTimer; set => ((IServerNode)_innerServerNode).ElectionTimer = value; }
    public System.Timers.Timer? HeartbeatTimer { get => ((IServerNode)_innerServerNode).HeartbeatTimer; set => ((IServerNode)_innerServerNode).HeartbeatTimer = value; }
    public Dictionary<long, IServerNode> IdToNode { get => ((IServerNode)_innerServerNode).IdToNode; set => ((IServerNode)_innerServerNode).IdToNode = value; }
    public Dictionary<long, bool?> IdToVotedForMe { get => ((IServerNode)_innerServerNode).IdToVotedForMe; set => ((IServerNode)_innerServerNode).IdToVotedForMe = value; }
    public DateTime ElectionTimerStartedAt { get => ((IServerNode)_innerServerNode).ElectionTimerStartedAt; set => ((IServerNode)_innerServerNode).ElectionTimerStartedAt = value; }
    public ServerSimulatioNode(ServerNode innerServerNode)
    {
        _innerServerNode = innerServerNode;
    }

    public Task AppendEntriesRPC(long senderId, int senderTerm)
    {
        Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
        {
            await ((IServerNode)_innerServerNode).AppendEntriesRPC(senderId, senderTerm);
        });

        return Task.CompletedTask;
    }

    public void StartNewElection()
    {
        ((IServerNode)_innerServerNode).StartNewElection();
    }

    public void StartNewElectionTimer(double electionTimeout = 0)
    {
        ((IServerNode)_innerServerNode).StartNewElectionTimer(electionTimeout);
    }

    public void AddNeighbors(List<IServerNode> neighbors)
    {
        ((IServerNode)_innerServerNode).AddNeighbors(neighbors);
    }

    public Task ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting)
    {
        Task.Delay(NetworkResponseDelay).ContinueWith(async (_previousTask) =>
        {
            await ((IServerNode)_innerServerNode).ResponseAppendEntriesRPC(senderId, isResponseRejecting); ;
        });

        return Task.CompletedTask;
    }

    public Task RequestVoteRPC(long senderId, int senderTerm)
    {
        Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
        {
            await ((IServerNode)_innerServerNode).RequestVoteRPC(senderId, senderTerm);
        });

        return Task.CompletedTask;
    }

    public Task ResponseRequestVoteRPC(long serverNodeId, bool wasVoteGiven)
    {
        Task.Delay(NetworkResponseDelay).ContinueWith(async (_previousTask) =>
        {
            await ((IServerNode)_innerServerNode).ResponseRequestVoteRPC(serverNodeId, wasVoteGiven);
        });

        return Task.CompletedTask;
    }

    public Task SendHeartBeat()
    {
        return ((IServerNode)_innerServerNode).SendHeartBeat();
    }

    public Task SendVotes()
    {
        return ((IServerNode)_innerServerNode).SendVotes();
    }

    public Task TransitionToLeader()
    {
        return ((IServerNode)_innerServerNode).TransitionToLeader();
    }

    public Task TransitionToCandidate()
    {
        return ((IServerNode)_innerServerNode).TransitionToCandidate();
    }

    public Task TransitionToPaused()
    {
        return ((IServerNode)_innerServerNode).TransitionToPaused();
    }

    public Task TransitionToFollower()
    {
        return ((IServerNode)_innerServerNode).TransitionToFollower();
    }
}