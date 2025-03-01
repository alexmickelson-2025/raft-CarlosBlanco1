﻿using myclasslib;
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
        NodeId = nodeId.HasValue ? nodeId.Value : DateTime.UtcNow.Ticks;

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


    public async Task StartTheThing()
    {
        StartNewElectionTimer();
        Logs = await LogManager.LoadLogsAsync(NodeId);
    }

    public void StartNewElectionTimer(double electionTimeout = 0)
    {
        ElectionTimer?.Stop();
        Random rand = new();
        double randomValueFrom150To300 = rand.Next((int)timeoutForTimer * 150, (int)timeoutForTimer * 300);

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

        if (State == ServerState.Paused)
        {
            return;
        }

        if (data.senderId == NodeId)
        {
            return;
        }

        if (!IdToNode.ContainsKey(data.senderId))
        {
            return;
        }


        IServerNode potentialLeader = IdToNode[data.senderId];

        if(data.prevLogIndex > Logs.Count)
        {
            await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(NodeId, true, CurrentTerm, CommitIndex, null));
            return;
        }

        if (data.senderTerm >= CurrentTerm)
        {
            CurrentTerm = data.senderTerm;
            State = ServerState.Follower;
            LeaderNodeId = data.senderId;
            wasVoteRequestedForThisTerm = false;

            StartNewElectionTimer();

            if (data.newEntries != null)
            {
                if (Logs.Count == 0)
                {
                    Logs.AddRange(data.newEntries);
                    await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(NodeId, false, CurrentTerm, CommitIndex, data.prevLogIndex));
                    CommitEntry(data.highestCommittedIndex);
                }
                else if (data.prevLogIndex < Logs.Count)
                {
                    LogEntry entryForThisNode = Logs[data.prevLogIndex.Value];
                    if (entryForThisNode.Term == data.prevLogTerm)
                    {
                        Logs.RemoveRange(data.prevLogIndex.Value, Logs.Count - data.prevLogIndex.Value);
                        Logs.AddRange(data.newEntries);

                        await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(NodeId, false, CurrentTerm, CommitIndex, data.prevLogIndex));
                        CommitEntry(data.highestCommittedIndex);
                    }
                    else
                    {
                        await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(NodeId, true, CurrentTerm, CommitIndex, data.prevLogIndex));
                    }
                }
                else
                {
                    await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(NodeId, true, CurrentTerm, CommitIndex, data.prevLogIndex));
                }
            }
            else
            {
                // Doesn't reject if the leader didn't send any new entries
                await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(NodeId, false, CurrentTerm, CommitIndex, data.prevLogIndex));
                CommitEntry(data.highestCommittedIndex);
            }
        }
        else
        {
            await potentialLeader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(NodeId, true, CurrentTerm, CommitIndex, null));
        }
    }



    public async Task ResponseAppendEntriesRPC(ResponseAppendEntriesDTO data)
    {
        if (data.senderTerm > CurrentTerm)
        {
            CurrentTerm = data.senderTerm.Value;
            await TransitionToFollower();
            return;
        }
        else
        {
            if (data.isResponseRejecting)
            {
                //If it rejects, it will decrease the prevlogindex by one
                if (IdToNextIndex[data.senderId] > 0)
                {
                    IdToNextIndex[data.senderId] -= 1;
                }
            }
            else
            {
                //If it has value it means it responded to an actual append entries request, not empty one
                if (data.ackedLogIndex.HasValue)
                {
                    //increase prevLogIndex by one
                    IdToNextIndex[data.senderId] += 1;
                    IdToLogValidationStatus[data.senderId] = true;

                    int majorityNum = (IdToNode.Count / 2) + 1;
                    int nodesThatValidated = IdToLogValidationStatus.Count(x => x.Value == true);

                    if (nodesThatValidated >= majorityNum)
                    {
                        if (data.ackedLogIndex.HasValue) CommitEntry(data.ackedLogIndex.Value);
                        //Clean the dictionary keeping track of nodes that accepted new log
                        foreach (var key in IdToLogValidationStatus.Keys.ToList())
                        {
                            IdToLogValidationStatus[key] = false;
                        }
                    }
                }
            }

        }
    }
    public async Task CommitEntry(int newCommitIndex)
    {
        if (newCommitIndex < 0) return;

        CommitIndex = newCommitIndex > CommitIndex ? newCommitIndex : CommitIndex;

        List<LogEntry> entriesToCommit = Logs.GetRange(0, Math.Max(CommitIndex, Logs.Count));

        foreach (var entry in entriesToCommit)
        {
            string command = entry.Command.Replace(" ", "");
            int key = (int)char.GetNumericValue(command[3]);

            string value = command.Substring(6);

            InternalStateMachine[key] = value;
        }

        await LogManager.SaveLogsAsync(NodeId, Logs);


        if (LeaderNodeId == NodeId) SendConfirmationResponseToClient();
    }

    public async void StartNewElection()
    {
        numberOfElectionsCalled += 1;
        CurrentTerm += 1;

        await TransitionToCandidate();

        if (IdToNode.Count <= 1)
        {
            State = ServerState.Candidate;
            return;
        }

    }
    public Task TransitionToLeader()
    {
        foreach (var idAndNode in IdToNode)
        {
            if (IdToNextIndex.ContainsKey(idAndNode.Key))
            {
                IdToNextIndex[idAndNode.Key] = -1;
            }
            else
            {
                IdToNextIndex.Add(idAndNode.Key, -1);
            }
        }

        if(ElectionTimer != null) ElectionTimer?.Stop();

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
        if(ElectionTimer != null) ElectionTimer?.Stop();

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
        if (Logs.Count > 0 && Logs.Count != CommitIndex + 1)
        {
            foreach (var idAndNode in IdToNode)
            {
                if (idAndNode.Value.NodeId != NodeId)
                {
                    int previousLogIndexForNode = IdToNextIndex[idAndNode.Key] < 0 ? 0 : IdToNextIndex[idAndNode.Key];
                    int previousLogTermForNode = Logs[previousLogIndexForNode].Term;
                    List<LogEntry> entriesForNode = Logs.GetRange(previousLogIndexForNode, Logs.Count - previousLogIndexForNode);
                    await idAndNode.Value.AppendEntriesRPC(new AppendEntriesDTO(NodeId, CurrentTerm, CommitIndex, entriesForNode, previousLogIndexForNode, previousLogTermForNode));
                }
            }
            //only send new logs in case not all the ones we have are commited
        }
        else
        {
            foreach (var idAndNode in IdToNode)
            {
                if (idAndNode.Value.NodeId != NodeId)
                {
                    //If all logs are already commited, send empty appendentriesRPC
                    await idAndNode.Value.AppendEntriesRPC(new AppendEntriesDTO(NodeId, CurrentTerm, CommitIndex));
                }
            }
        }

    }
    public async Task SendVotes()
    {
        foreach (var idAndNode in IdToNode)
        {
            if (idAndNode.Key != NodeId) 
            {
                await idAndNode.Value.RequestVoteRPC(new RequestVoteDTO ( NodeId, CurrentTerm ));
            }
        }
    }

    public async Task ResponseRequestVoteRPC(ResponseRequestVoteDTO data)
    {
        if (!data.wasVoteGiven)
        {
            CurrentTerm = data.serverNodeTerm;
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

        if (data.senderId == NodeId)
        {
            return;
        }

        if (!IdToNode.ContainsKey(data.senderId))
        {
            return;
        }

        IServerNode nodeRequestingVote = IdToNode[data.senderId];

        if (data.senderTerm < CurrentTerm || wasVoteRequestedForThisTerm)
        {
            await nodeRequestingVote.ResponseRequestVoteRPC(new ResponseRequestVoteDTO ( NodeId, CurrentTerm, false ));
        }
        else
        {
            await nodeRequestingVote.ResponseRequestVoteRPC(new ResponseRequestVoteDTO ( NodeId, CurrentTerm, true ));
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