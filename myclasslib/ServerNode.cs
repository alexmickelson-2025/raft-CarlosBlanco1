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

    public async Task AppendEntriesRPC(long senderId, int senderTerm, List<LogEntry>? entries, int? entryIndex, int? highestCommittedIndex)
    {
        if (State == ServerState.Paused) return;

        IServerNode potentialLeader = IdToNode[senderId];

        if (senderTerm > CurrentTerm)
        {
            CurrentTerm = senderTerm;
            State = ServerState.Follower;
            LeaderNodeId = senderId;
            wasVoteRequestedForThisTerm = false;
            ElectionTimer?.Stop();
            StartNewElectionTimer();
        }
        else if (senderTerm < CurrentTerm)
        {
            await potentialLeader.ResponseAppendEntriesRPC(NodeId, true, CurrentTerm, CommitIndex, null);
            return;
        }

        ElectionTimer?.Stop();
        StartNewElectionTimer();

        if (highestCommittedIndex.HasValue && highestCommittedIndex > CommitIndex && highestCommittedIndex > -1 && entries != null)
        {
            CommitIndex = highestCommittedIndex.Value;

            Console.WriteLine($"For {NodeId}, entries.count is {entries.Count} and CommitIndex is: {CommitIndex}");

            if (CommitIndex < entries.Count) CommitEntry(entries[CommitIndex], CommitIndex);
        }

        if (entryIndex == null || entryIndex.Value < 0)
        {
            await potentialLeader.ResponseAppendEntriesRPC(NodeId, true, CurrentTerm, CommitIndex, null);
            return;
        }

        if (entryIndex < Logs.Count && entries != null && Logs[entryIndex.Value].Term != entries[0].Term)
        {
            Logs.RemoveRange(entryIndex.Value, Logs.Count - entryIndex.Value);
        }

        foreach (var entry in entries)
        {
            if (entries.IndexOf(entry) >= Logs.Count)
            {
                Logs.Add(entry);
            }
            else
            {
                Logs[entries.IndexOf(entry)] = entry;
            }
        }

        await potentialLeader.ResponseAppendEntriesRPC(NodeId, false, CurrentTerm, CommitIndex, entryIndex);
    }


    public async Task ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting, int? senderTerm = 0, int? commitIndex = 0, int? ackedLogIndex = null)
    {
        if (senderTerm > CurrentTerm)
        {
            CurrentTerm = senderTerm.Value;
            await TransitionToFollower();
            return;
        }

        if (isResponseRejecting)
        {
            if (IdToNextIndex[senderId] > 1)
            {
                IdToNextIndex[senderId] -= 1;
            }
            return;
        }

        if (!ackedLogIndex.HasValue)
        {
            return;
        }

        if (!IdToLogValidationStatus.ContainsKey(senderId))
        {
            IdToLogValidationStatus[senderId] = false;
        }

        IdToLogValidationStatus[senderId] = true;

        int majorityNum = (IdToNode.Count / 2) + 1;
        int nodesThatValidated = IdToLogValidationStatus.Count(x => x.Value == true);

        if (nodesThatValidated >= majorityNum)
        {
            int lastLogIndex = Logs.Count > 0 ? Logs.Count - 1 : 0;

            if (commitIndex.HasValue && commitIndex.Value <= lastLogIndex && commitIndex == -1 && Logs.Count > 0) CommitEntry(Logs[0], 0);
            if (commitIndex.HasValue && commitIndex.Value <= lastLogIndex && commitIndex > -1) CommitEntry(Logs[commitIndex.Value], null);
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
        await TransitionToCandidate();
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
            await idAndNode.Value.AppendEntriesRPC(NodeId, CurrentTerm, Logs, mostRecentIndex, CommitIndex);
        }
    }
    public async Task SendVotes()
    {
        foreach (var idAndNode in IdToNode)
        {
            if (idAndNode.Value.NodeId == NodeId) continue;
            await idAndNode.Value.RequestVoteRPC(NodeId, CurrentTerm);
        }
    }

    public async Task ResponseRequestVoteRPC(long serverNodeId, bool wasVoteGiven)
    {
        if (!wasVoteGiven)
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
        if (State == ServerState.Paused) return;

        IServerNode nodeRequestingVote = IdToNode[senderId];

        if (senderTerm < CurrentTerm || wasVoteRequestedForThisTerm)
        {
            await nodeRequestingVote.ResponseRequestVoteRPC(NodeId, false);
        }
        else
        {
            await nodeRequestingVote.ResponseRequestVoteRPC(NodeId, true);
        }

        if (senderTerm == CurrentTerm) wasVoteRequestedForThisTerm = true;

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