using myclasslib;
using System.Threading.Tasks;
using System.Timers;

public class ServerNode : IServerNode
{
    public long NodeId { get; set; }
    public long LeaderNodeId { get; set; }
    public ServerState State { get; set; }
    public int CurrentTerm { get; set; }
    public System.Timers.Timer? ElectionTimer { get; set; }
    public System.Timers.Timer? HeartbeatTimer { get; set; }
    public Dictionary<long, IServerNode> IdToNode { get; set; }
    public Dictionary<long, bool?> IdToVotedForMe { get; set; }
    public DateTime ElectionTimerStartedAt { get; set; }
    public int NextIndex { get; set; }
    public List<LogEntry> Logs {get; set;}
    public int CommitIndex { get; set;}
    public Dictionary<long, int> IdToNextIndex { get; set;}
    public Dictionary<long, bool?> IdToLogValidationStatus { get; set; }

    public int numberOfElectionsCalled = 0;
    public bool wasVoteRequestedForThisTerm = false;
    public bool wasResponseToClientSent = false;
    public double timeoutForTimer = 1;
    public ServerNode(List<IServerNode> neighbors, int startTerm = 1, int electionTimeout = 0, bool startElectionTimer = true)
    {
        NodeId = DateTime.UtcNow.Ticks;
        State = ServerState.Follower;
        Logs = [];

        IdToNode = [];
        IdToNode[NodeId] = this;

        IdToVotedForMe = [];
        IdToVotedForMe[NodeId] = null;

        IdToLogValidationStatus = [];
        IdToLogValidationStatus[NodeId] = null;
                
        IdToNextIndex = [];

        CommitIndex = 0;
        CurrentTerm = startTerm;

        AddNeighbors(neighbors);
    }

    public void StartTheThing()
    {
        StartNewElectionTimer();
    }

    public void StartNewElectionTimer(double electionTimeout = 0)
    {
        Random rand = new();
        double randomValueFrom150To300 = rand.Next(150, 300);

        if (ElectionTimer == null)
        {
            ElectionTimer = new System.Timers.Timer();
        }

        ElectionTimer.Interval = electionTimeout == 0 ? randomValueFrom150To300 * timeoutForTimer: electionTimeout * timeoutForTimer;
        ElectionTimer.AutoReset = false;

        ElectionTimerStartedAt = DateTime.Now;
        ElectionTimer.Elapsed += (sender, e) => {StartNewElection();};
        ElectionTimer.Start();

    }

    public async Task AppendEntriesRPC(long senderId, int senderTerm, LogEntry? entry = null, int? highestCommitedIndex = 0)
    {
        if (State == ServerState.Paused) return;

        IServerNode potentialLeader = IdToNode[senderId];

        if (senderTerm > CurrentTerm)
        {
            ElectionTimer?.Stop();
            StartNewElectionTimer();

            State = ServerState.Follower;
            CurrentTerm = senderTerm;
            wasVoteRequestedForThisTerm = false;
            LeaderNodeId = senderId;

            if(entry != null) Logs.Add(entry);

            await potentialLeader.ResponseAppendEntriesRPC(NodeId, false, CurrentTerm, CommitIndex);
        }
        else if (senderTerm == CurrentTerm) 
        {
            ElectionTimer?.Stop();
            StartNewElectionTimer();

            State = ServerState.Follower;
            LeaderNodeId = senderId;

            if(entry != null) Logs.Add(entry);

            await potentialLeader.ResponseAppendEntriesRPC(NodeId, false, CurrentTerm, CommitIndex);
        }
        else
        {
            if(senderTerm < CurrentTerm)
            {
                await potentialLeader.ResponseAppendEntriesRPC(NodeId, true, CurrentTerm, CommitIndex);
            }
            else
            {
                return;
            }
        }
    }

    public async Task ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting, int? senderTerm = 0, int? commitIndex = 0)
    {
        if(isResponseRejecting)
        {
            CurrentTerm = IdToNode[senderId].CurrentTerm;
            await TransitionToFollower();
        }
        else
        {
            int majorityNum = (IdToNode.Count / 2) + 1;

            if (IdToVotedForMe.ContainsKey(senderId))
            {
                //so if it didnt reject, it means its valid
                IdToLogValidationStatus[senderId] = !isResponseRejecting;
                int nodesThatValidated = IdToLogValidationStatus.Where(x => x.Value == true).Count();

                if (nodesThatValidated >= majorityNum)
                {
                    CommitIndex++;
                    SendConfirmationResponseToClient();
                }
            }
            else
            {
                throw new Exception("This serverNode wasn't passed in as a neighbor at initialization");
            }

            return;
        }
    }

    public async void StartNewElection()
    {
        numberOfElectionsCalled += 1;

        await TransitionToCandidate();

        if (IdToNode.Count <= 1)
        {
            State = ServerState.Candidate;
            return;
        }

        CurrentTerm += 1;
        await TransitionToCandidate();
    }
    public Task TransitionToLeader()
    {
        foreach (var idAndNode in IdToNode)
        {
            IdToNextIndex[idAndNode.Key] = Logs.Count == 0? 0 : Logs.Count; 
        }

        ElectionTimer?.Stop();

        State = ServerState.Leader;
        LeaderNodeId = NodeId;

        if (HeartbeatTimer == null)
        {
            HeartbeatTimer = new System.Timers.Timer();
        }

        HeartbeatTimer.Interval = 20 * timeoutForTimer;
        HeartbeatTimer.AutoReset = true;
        HeartbeatTimer.Elapsed += async (sender, e) => { if(State == ServerState.Leader) await SendHeartBeat(); };
        HeartbeatTimer.Start();

        return Task.CompletedTask;
    }
    public async Task TransitionToCandidate()
    {
        ElectionTimer?.Stop();

        State = ServerState.Candidate;

        StartNewElectionTimer();
        IdToVotedForMe[NodeId] = true;

        if (IdToNode.Count > 1)
        {
            await SendVotes();
        }

        return;
    }
    public async Task SendHeartBeat()
    {
        LogEntry? mostRecent = Logs.Count == 0? null : Logs[^1];

        foreach (var idAndNode in IdToNode)
        {
            if(idAndNode.Value.NodeId == NodeId) continue;
            await idAndNode.Value.AppendEntriesRPC(NodeId, CurrentTerm, mostRecent, CommitIndex);
        }
    }
    public async Task SendVotes()
    {
        foreach (var idAndNode in IdToNode)
        {
            if(idAndNode.Value.NodeId == NodeId) continue;
            await idAndNode.Value.RequestVoteRPC(NodeId, CurrentTerm);
        }
    }

    public async Task ResponseRequestVoteRPC(long serverNodeId, bool wasVoteGiven)
    {
        if(!wasVoteGiven)
        {
            CurrentTerm = IdToNode[serverNodeId].CurrentTerm;
            wasVoteRequestedForThisTerm = false;
        }

        int votesNeededToWinTheElection = (IdToNode.Count / 2) + 1;

        if (IdToVotedForMe.ContainsKey(serverNodeId))
        {
            IdToVotedForMe[serverNodeId] = wasVoteGiven;
            int notesThatveVotedForMe = IdToVotedForMe.Where(x => x.Value == true).Count();

            if (notesThatveVotedForMe >= votesNeededToWinTheElection)
            {
                await TransitionToLeader();
            }
        }
        else
        {
            throw new Exception("This serverNode wasn't passed in as a neighbor at initialization");
        }

        return;
    }
    public async Task RequestVoteRPC(long senderId, int senderTerm)
    {
        if(State == ServerState.Paused) return;

        IServerNode nodeRequestingVote = IdToNode[senderId];

        if(senderTerm < CurrentTerm || wasVoteRequestedForThisTerm) 
        {
            await nodeRequestingVote.ResponseRequestVoteRPC(NodeId, false);
        }
        else
        {
            await nodeRequestingVote.ResponseRequestVoteRPC(NodeId, true);
        }

        if(senderTerm == CurrentTerm) wasVoteRequestedForThisTerm = true;

        return;
    }

    public Task TransitionToPaused()
    {
        State = ServerState.Paused;
        ElectionTimer?.Stop();
        HeartbeatTimer?.Stop();

        return Task.CompletedTask;
    }

    public void AddNeighbors(List<IServerNode> neighbors)
    {
        foreach (var neighbor in neighbors)
        {
            IdToNode.Add(neighbor.NodeId, neighbor);
            IdToVotedForMe.Add(neighbor.NodeId, null);
        }
    }

    public Task TransitionToFollower()
    {
        State = ServerState.Follower;
        HeartbeatTimer?.Stop();
        ElectionTimer?.Stop();

        StartNewElectionTimer();

        return Task.CompletedTask;
    }

    public void SendCommandToLeader(LogEntry entry)
    {
        Logs.Add(entry);
        IdToLogValidationStatus[NodeId] = true;
    }

    public void SendConfirmationResponseToClient()
    {
        wasResponseToClientSent = true;
    }
}