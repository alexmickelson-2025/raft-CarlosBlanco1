using myclasslib;
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
    public int numberOfElectionsCalled = 0;
    public bool wasVoteRequestedForThisTerm = false;
    public ServerNode(List<IServerNode> neighbors, int startTerm = 1, int electionTimeout = 0, bool startElectionTimer = true, ServerState initialState = ServerState.Follower)
    {
        NodeId = DateTime.UtcNow.Ticks;

        IdToNode = [];
        IdToNode[NodeId] = this;

        IdToVotedForMe = [];
        IdToVotedForMe[NodeId] = null;

        CurrentTerm = startTerm;

        AddNeighbors(neighbors);

        switch (initialState)
        {
            case ServerState.Follower:
                State = ServerState.Follower;
                if(startElectionTimer)
                {
                    StartNewElectionTimer(electionTimeout);
                }
                break;
            case ServerState.Leader:
                TransitionToLeader();
                break;
            case ServerState.Candidate:
                TransitionToCandidate();
                break;
            case ServerState.Paused:
                TransitionToPaused();
                break;
            default:
                break;
        }

    }

    public void StartNewElectionTimer(double electionTimeout = 0)
    {
        Random rand = new();
        double randomValueFrom150To300 = rand.Next(150, 300);

        ElectionTimer = new System.Timers.Timer(electionTimeout == 0 ? randomValueFrom150To300 : electionTimeout)
        {
            AutoReset = false
        };

        ElectionTimerStartedAt = DateTime.Now;
        ElectionTimer.Elapsed += (sender, e) => StartNewElection();
        ElectionTimer.Start();
    }

    public Task AppendEntriesRPC(long senderId, int senderTerm)
    {
        if(State == ServerState.Paused) return Task.CompletedTask;

        IServerNode potentialLeader = IdToNode[senderId];

        if (senderTerm >= CurrentTerm)
        {
            ElectionTimer?.Stop();
            StartNewElectionTimer();

            State = ServerState.Follower;
            CurrentTerm = senderTerm;

            LeaderNodeId = senderId;
            potentialLeader.ResponseAppendEntriesRPC(NodeId, false);
        }
        else
        {
            potentialLeader.ResponseAppendEntriesRPC(NodeId, true);
        }

        return Task.CompletedTask;
    }

    public Task ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting)
    {
        if(isResponseRejecting)
        {
            CurrentTerm = IdToNode[senderId].CurrentTerm;
            TransitionToFollower();
        }

        return Task.CompletedTask;
    }

    public void StartNewElection()
    {
        numberOfElectionsCalled += 1;

        if (IdToNode.Count <= 1)
        {
            Console.WriteLine("had no neighbors");
            return;
        }

        CurrentTerm += 1;
        TransitionToCandidate();
    }
    public void TransitionToLeader()
    {
        ElectionTimer?.Stop();

        State = ServerState.Leader;

        HeartbeatTimer = new System.Timers.Timer(20) { AutoReset = true };
        HeartbeatTimer.Elapsed += (sender, e) => SendHeartBeat();
        HeartbeatTimer.Start();
    }
    public void TransitionToCandidate()
    {
        ElectionTimer?.Stop();

        State = ServerState.Candidate;

        StartNewElectionTimer();
        IdToVotedForMe[NodeId] = true;

        if (IdToNode.Count > 1)
        {
            SendVotes();
        }
    }
    public void SendHeartBeat()
    {
        foreach (var idAndNode in IdToNode)
        {
            if(idAndNode.Value.NodeId == NodeId) continue;
            idAndNode.Value.AppendEntriesRPC(NodeId, CurrentTerm);
        }
    }
    public void SendVotes()
    {
        foreach (var idAndNode in IdToNode)
        {
            if(idAndNode.Value.NodeId == NodeId) continue;
            idAndNode.Value.RequestVoteRPC(NodeId, CurrentTerm);
        }
    }

    public Task ResponseRequestVoteRPC(long serverNodeId, bool wasVoteGiven)
    {
        if(!wasVoteGiven) CurrentTerm = IdToNode[serverNodeId].CurrentTerm;

        int votesNeededToWinTheElection = (IdToNode.Count / 2) + 1;

        if (IdToVotedForMe.ContainsKey(serverNodeId))
        {
            IdToVotedForMe[serverNodeId] = wasVoteGiven;
            int notesThatveVotedForMe = IdToVotedForMe.Where(x => x.Value == true).Count();

            if (notesThatveVotedForMe >= votesNeededToWinTheElection)
            {
                TransitionToLeader();
            }
        }
        else
        {
            throw new Exception("This serverNode wasn't passed in as a neighbor at initialization");
        }

        return Task.CompletedTask;
    }
    public Task RequestVoteRPC(long senderId, int senderTerm)
    {
        if(State == ServerState.Paused) return Task.CompletedTask;

        IServerNode nodeRequestingVote = IdToNode[senderId];

        if(senderTerm < CurrentTerm || wasVoteRequestedForThisTerm) 
        {
            nodeRequestingVote.ResponseRequestVoteRPC(NodeId, false);
        }
        else
        {
            nodeRequestingVote.ResponseRequestVoteRPC(NodeId, true);
        }

        if(senderTerm == CurrentTerm) wasVoteRequestedForThisTerm = true;

        return Task.CompletedTask;
    }

    public void TransitionToPaused()
    {
        State = ServerState.Paused;
        ElectionTimer?.Stop();
        HeartbeatTimer?.Stop();
    }

    public void AddNeighbors(List<IServerNode> neighbors)
    {
        foreach (var neighbor in neighbors)
        {
            IdToNode.Add(neighbor.NodeId, neighbor);
            IdToVotedForMe.Add(neighbor.NodeId, null);
        }
    }

    public void TransitionToFollower()
    {
        State = ServerState.Follower;
        HeartbeatTimer?.Stop();
        ElectionTimer?.Stop();

        StartNewElectionTimer();
    }
}