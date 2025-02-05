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
    public List<LogEntry> Logs { get; set; }
    public int CommitIndex { get; set; }
    public Dictionary<long, int> IdToNextIndex { get; set; }
    public Dictionary<long, bool?> IdToLogValidationStatus { get; set; }
    public Dictionary<int, string> InternalStateMachine { get; set; }
    public int numberOfElectionsCalled = 0;
    public bool wasVoteRequestedForThisTerm = false;
    public bool wasResponseToClientSent = false;
    public double timeoutForTimer = 1;
    public ServerNode(List<IServerNode> neighbors, int startTerm = 1, int electionTimeout = 0, bool startElectionTimer = true, long? nodeId = null)
    {
        NodeId = nodeId ?? DateTime.UtcNow.Ticks;
        Console.WriteLine(NodeId);

        State = ServerState.Follower;
        Logs = [];

        IdToNode = [];
        IdToNode[NodeId] = this;

        IdToVotedForMe = [];
        IdToVotedForMe[NodeId] = null;

        IdToLogValidationStatus = [];
        IdToLogValidationStatus[NodeId] = null;

        IdToNextIndex = [];
        InternalStateMachine = [];

        CommitIndex = -1;
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

        ElectionTimer.Interval = electionTimeout == 0 ? randomValueFrom150To300 * timeoutForTimer : electionTimeout * timeoutForTimer;
        ElectionTimer.AutoReset = false;

        ElectionTimerStartedAt = DateTime.Now;
        ElectionTimer.Elapsed += (sender, e) => { StartNewElection(); };
        ElectionTimer.Start();

    }

    public async Task AppendEntriesRPC(AppendEntriesDTO data)
    {
        Console.WriteLine($"received append entries request from node {data.senderId}");

        if (State == ServerState.Paused) return;

        IServerNode potentialLeader = IdToNode[data.senderId];

        if (data.senderTerm > CurrentTerm)
        {
            CurrentTerm = data.senderTerm;
            State = ServerState.Follower;
            LeaderNodeId = data.senderId;
            wasVoteRequestedForThisTerm = false;
            ElectionTimer?.Stop();
            StartNewElectionTimer();
        }
        else if (data.senderTerm < CurrentTerm)
        {
            await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(NodeId, true, CurrentTerm, CommitIndex, null));
            return;
        }

        ElectionTimer?.Stop();
        StartNewElectionTimer();

        if (data.highestCommittedIndex.HasValue && data.highestCommittedIndex > CommitIndex && data.highestCommittedIndex > -1 && data.entries != null)
        {
            CommitIndex = data.highestCommittedIndex.Value;

            Console.WriteLine($"For {NodeId}, entries.count is {data.entries.Count} and CommitIndex is: {CommitIndex}");

            if (CommitIndex < data.entries.Count) CommitEntry(data.entries[CommitIndex], CommitIndex);
        }

        if (data.entryIndex == null || data.entryIndex.Value < 0)
        {
            await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO (NodeId, true, CurrentTerm, CommitIndex, null ));
            return;
        }

        if (data.entryIndex < Logs.Count && data.entries != null && Logs[data.entryIndex.Value].Term != data.entries[0].Term)
        {
            Logs.RemoveRange(data.entryIndex.Value, Logs.Count - data.entryIndex.Value);
        }

        if (data.entries != null)
        {
            foreach (var entry in data.entries)
            {
                if (data.entries.IndexOf(entry) >= Logs.Count)
                {
                    Logs.Add(entry);
                }
                else
                {
                    Logs[data.entries.IndexOf(entry)] = entry;
                }
            }
        }

        await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO ( NodeId, false, CurrentTerm, CommitIndex, data.entryIndex ));
    }


    public async Task ResponseAppendEntriesRPC(ResponseAppendEntriesDTO data)
    {
        if (data.senderTerm > CurrentTerm)
        {
            CurrentTerm = data.senderTerm.Value;
            await TransitionToFollower();
            return;
        }

        if (data.isResponseRejecting)
        {
            if (IdToNextIndex[data.senderId] > 1)
            {
                IdToNextIndex[data.senderId] -= 1;
            }
            return;
        }

        if (!data.ackedLogIndex.HasValue)
        {
            return;
        }

        if (!IdToLogValidationStatus.ContainsKey(data.senderId))
        {
            IdToLogValidationStatus[data.senderId] = false;
        }

        IdToLogValidationStatus[data.senderId] = true;

        int majorityNum = (IdToNode.Count / 2) + 1;
        int nodesThatValidated = IdToLogValidationStatus.Count(x => x.Value == true);

        if (nodesThatValidated >= majorityNum)
        {
            int lastLogIndex = Logs.Count > 0 ? Logs.Count - 1 : 0;

            if (data.commitIndex.HasValue && data.commitIndex.Value <= lastLogIndex && data.commitIndex == -1 && Logs.Count > 0) CommitEntry(Logs[0], 0);
            if (data.commitIndex.HasValue && data.commitIndex.Value <= lastLogIndex && data.commitIndex > -1) CommitEntry(Logs[data.commitIndex.Value], null);
        }
    }


    public void CommitEntry(LogEntry? entry, int? newCommitIndex)
    {
        Console.WriteLine($"running for {NodeId}");
        CommitIndex = newCommitIndex.HasValue ? newCommitIndex.Value : CommitIndex + 1;

        if (LeaderNodeId == NodeId) SendConfirmationResponseToClient();

        if (entry != null)
        {

            string command = entry.Command.Replace(" ", "");
            int key = (int)char.GetNumericValue(command[3]);

            string value = command.Substring(6);

            InternalStateMachine[key] = value;
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
    }
    public Task TransitionToLeader()
    {
        foreach (var idAndNode in IdToNode)
        {
            IdToNextIndex[idAndNode.Key] = Logs.Count == 0 ? 0 : Logs.Count;
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
        HeartbeatTimer.Elapsed += async (sender, e) => { if (State == ServerState.Leader) await SendHeartBeat(); };
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
        int mostRecentIndex = Logs.Count == 0 ? 0 : Logs.Count - 1;

        foreach (var idAndNode in IdToNode)
        {
            if (idAndNode.Value.NodeId == NodeId) continue;
            await idAndNode.Value.AppendEntriesRPC(new AppendEntriesDTO (NodeId, CurrentTerm, Logs, mostRecentIndex, CommitIndex ));
        }
    }
    public async Task SendVotes()
    {
        foreach (var idAndNode in IdToNode)
        {
            if (idAndNode.Key != NodeId) await idAndNode.Value.RequestVoteRPC(new RequestVoteDTO { senderId = NodeId, senderTerm = CurrentTerm });
        }
    }

    public async Task ResponseRequestVoteRPC(ResponseRequestVoteDTO data)
    {
        if (!data.wasVoteGiven)
        {
            CurrentTerm = IdToNode[data.serverNodeId].CurrentTerm;
            wasVoteRequestedForThisTerm = false;
        }

        int votesNeededToWinTheElection = (IdToNode.Count / 2) + 1;

        if (IdToVotedForMe.ContainsKey(data.serverNodeId))
        {
            IdToVotedForMe[data.serverNodeId] = data.wasVoteGiven;
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
    public async Task RequestVoteRPC(RequestVoteDTO data)
    {
        if (State == ServerState.Paused) return;

        Console.WriteLine($" I received a vote request from {data.senderId}");
        foreach (var kvp in IdToNode)
        {
            Console.WriteLine($" I got a neighbor with id {kvp.Key}");
        }

        IServerNode nodeRequestingVote = IdToNode[data.senderId];

        if (data.senderTerm < CurrentTerm || wasVoteRequestedForThisTerm)
        {
            await nodeRequestingVote.ResponseRequestVoteRPC(new ResponseRequestVoteDTO { serverNodeId = NodeId, wasVoteGiven = false });
        }
        else
        {
            await nodeRequestingVote.ResponseRequestVoteRPC(new ResponseRequestVoteDTO { serverNodeId = NodeId, wasVoteGiven = true });
        }

        if (data.senderTerm == CurrentTerm) wasVoteRequestedForThisTerm = true;

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

    public bool SendCommandToLeader(LogEntry entry)
    {
        if (NodeId != LeaderNodeId) return false;
        Logs.Add(entry);
        IdToLogValidationStatus[NodeId] = true;

        return true;
    }

    public void SendConfirmationResponseToClient()
    {
        wasResponseToClientSent = true;
    }
}