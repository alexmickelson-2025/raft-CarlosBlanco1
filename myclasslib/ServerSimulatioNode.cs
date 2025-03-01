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
    public System.Timers.Timer ElectionTimer { get => ((IServerNode)_innerServerNode).ElectionTimer!; set => ((IServerNode)_innerServerNode).ElectionTimer = value; }
    public System.Timers.Timer? HeartbeatTimer { get => ((IServerNode)_innerServerNode).HeartbeatTimer; set => ((IServerNode)_innerServerNode).HeartbeatTimer = value; }
    public Dictionary<long, IServerNode> IdToNode { get => ((IServerNode)_innerServerNode).IdToNode; set => ((IServerNode)_innerServerNode).IdToNode = value; }
    public Dictionary<long, bool?> IdToVotedForMe { get => ((IServerNode)_innerServerNode).IdToVotedForMe; set => ((IServerNode)_innerServerNode).IdToVotedForMe = value; }
    public DateTime ElectionTimerStartedAt { get => ((IServerNode)_innerServerNode).ElectionTimerStartedAt; set => ((IServerNode)_innerServerNode).ElectionTimerStartedAt = value; }
    public int CommitIndex { get => ((IServerNode)_innerServerNode).CommitIndex; set => ((IServerNode)_innerServerNode).CommitIndex = value; }
    public Dictionary<long, int> IdToNextIndex { get => ((IServerNode)_innerServerNode).IdToNextIndex; set => ((IServerNode)_innerServerNode).IdToNextIndex = value; }
    public Dictionary<long, bool?> IdToLogValidationStatus { get => ((IServerNode)_innerServerNode).IdToLogValidationStatus; set => ((IServerNode)_innerServerNode).IdToLogValidationStatus = value; }
    public bool isPaused {get; set;} = false;
    public Dictionary<int, string> InternalStateMachine { get => ((IServerNode)_innerServerNode).InternalStateMachine; set => ((IServerNode)_innerServerNode).InternalStateMachine = value; }
    public List<LogEntry> Logs { get => ((IServerNode)_innerServerNode).Logs; set => ((IServerNode)_innerServerNode).Logs = value; }

    public ServerSimulatioNode(ServerNode innerServerNode)
    {
        _innerServerNode = innerServerNode;
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

    public Task RequestVoteRPC(RequestVoteDTO data)
    {
        Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
        {
            await ((IServerNode)_innerServerNode).RequestVoteRPC(data);
        });

        return Task.CompletedTask;
    }

    public Task ResponseRequestVoteRPC(ResponseRequestVoteDTO data)
    {
        Task.Delay(NetworkResponseDelay).ContinueWith(async (_previousTask) =>
        {
            await ((IServerNode)_innerServerNode).ResponseRequestVoteRPC(data);
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
        isPaused = false;
        return ((IServerNode)_innerServerNode).TransitionToLeader();
    }

    public Task TransitionToCandidate()
    {
    isPaused = false;
        return ((IServerNode)_innerServerNode).TransitionToCandidate();
    }

    public Task TransitionToPaused()
    {
        isPaused = true;
        return ((IServerNode)_innerServerNode).TransitionToPaused();
    }

    public Task TransitionToFollower()
    {
        isPaused = false;
        return ((IServerNode)_innerServerNode).TransitionToFollower();
    }

    public bool SendCommandToLeader(LogEntry entry)
    {
        return ((IServerNode)_innerServerNode).SendCommandToLeader(entry);
    }

    public void SendConfirmationResponseToClient()
    {
        ((IServerNode)_innerServerNode).SendConfirmationResponseToClient();
    }

    public Task AppendEntriesRPC(AppendEntriesDTO data)
    {
        return ((IServerNode)_innerServerNode).AppendEntriesRPC(data);
    }

    public Task ResponseAppendEntriesRPC(ResponseAppendEntriesDTO data)
    {
        return ((IServerNode)_innerServerNode).ResponseAppendEntriesRPC(data);
    }
}