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
    public int numberOfElectionsCalled = 0;
    public bool wasVoteRequestedForThisTerm = false;
    public bool shouldISendHearbeats = false;
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
        ElectionTimer.Elapsed += (sender, e) => 
        {
            StartNewElection();
        };
        ElectionTimer.Start();
    }

    public async Task AppendEntriesRPC(long senderId, int senderTerm)
    {
        if (State == ServerState.Paused) return;

        IServerNode potentialLeader = IdToNode[senderId];

        if (senderTerm > CurrentTerm)
        {
            ElectionTimer?.Stop();
            StartNewElectionTimer();

            State = ServerState.Follower;
            CurrentTerm = senderTerm;
            LeaderNodeId = senderId;

            await potentialLeader.ResponseAppendEntriesRPC(NodeId, false);
        }
        else if (senderTerm == CurrentTerm) 
        {
            ElectionTimer?.Stop();
            StartNewElectionTimer();

            State = ServerState.Follower;
            LeaderNodeId = senderId;
            await potentialLeader.ResponseAppendEntriesRPC(NodeId, false);
        }
        else
        {
            Console.WriteLine($"Outdated term detected at {DateTime.Now}: My term is {CurrentTerm}, theirs is {senderTerm}");

            if(senderTerm < CurrentTerm)
            {
                Console.WriteLine("actually sent");
                await potentialLeader.ResponseAppendEntriesRPC(NodeId, true);
            }
        }
    }


    public Task ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting)
    {
        if(isResponseRejecting)
        {
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

        shouldISendHearbeats = true;

        HeartbeatTimer = new System.Timers.Timer(20) { AutoReset = true };
        HeartbeatTimer.Elapsed += async (sender, e) => 
        {
            if(shouldISendHearbeats) SendHeartBeat();
        };
        HeartbeatTimer.Start();

        return;
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

        return;
    }
    public async void SendHeartBeat()
    {
        foreach (var idAndNode in IdToNode)
        {
            if(idAndNode.Value.NodeId == NodeId) continue;
            await idAndNode.Value.AppendEntriesRPC(NodeId, CurrentTerm);
        }

        return;
    }
    public async void SendVotes()
    {
        foreach (var idAndNode in IdToNode)
        {
            if(idAndNode.Value.NodeId == NodeId) continue;
            await idAndNode.Value.RequestVoteRPC(NodeId, CurrentTerm);
        }

        return;
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
        shouldISendHearbeats = false;
        HeartbeatTimer?.Stop();
        ElectionTimer?.Stop();

        StartNewElectionTimer();
    }
}