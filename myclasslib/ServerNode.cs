namespace myclasslib;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

public class ServerNode : IServerNode
{
    public long NodeId { get; set; }
    public long LeaderNodeId { get; set; }
    public ServerState State { get; set; }
    public int CurrentTerm { get; set; }
    public Timer ElectionTimer { get; set; }
    public Timer? HeartbeatTimer { get; set; }
    public Dictionary<long, IServerNode> IdToNode { get; set; }
    public Dictionary<long, bool?> IdToVotedForMe { get; set; }
    public ServerNode(List<IServerNode> neighbors, int startTerm = 0, int electionTimeout = 0)
    {
        State = ServerState.Follower;
        NodeId = DateTime.UtcNow.Ticks;
        IdToNode = [];
        IdToNode[NodeId] = this;

        IdToVotedForMe = [];
        IdToVotedForMe[NodeId] = null;

        CurrentTerm = startTerm;

        foreach (var neighbor in neighbors)
        {
            IdToNode.Add(neighbor.NodeId, neighbor);
            IdToVotedForMe.Add(neighbor.NodeId, null);
        }

        ElectionTimer = CreateNewElectionTimer(electionTimeout);
        ElectionTimer.Elapsed += (sender, e) => StartNewElection();
        ElectionTimer.Start();
    }

    public Timer CreateNewElectionTimer(double electionTimeout = 0)
    {
        Random rand = new();
        double randomValueFrom150To300 = rand.Next(150, 300);

        var Timer = new Timer(electionTimeout == 0 ? randomValueFrom150To300 : electionTimeout)
        {
            AutoReset = false
        };

        return Timer;
    }

    public void AppendEntriesRPC(long senderId, int senderTerm)
    {
        if(State == ServerState.Paused) return;

        IServerNode potentialLeader = IdToNode[senderId];

        if (senderTerm >= CurrentTerm)
        {
            ElectionTimer.Stop();
            ElectionTimer.Start();

            LeaderNodeId = senderId;
            potentialLeader.ResponseAppendEntriesRPC(NodeId, false);
        }
        else
        {
            potentialLeader.ResponseAppendEntriesRPC(NodeId, true);
        }
    }

    public string ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting)
    {
        return "";
    }

    public void StartNewElection()
    {
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
        ElectionTimer.Stop();

        State = ServerState.Leader;

        HeartbeatTimer = new Timer(20) { AutoReset = false };

        HeartbeatTimer.Elapsed += (sender, e) => SendHeartBeat();
        HeartbeatTimer.Start();
    }
    public void TransitionToCandidate()
    {
        State = ServerState.Candidate;
        ResponseRequestVoteRPC(NodeId, true);

        if (IdToNode.Count > 1)
        {
            SendVotes();
        }
    }
    public void SendHeartBeat()
    {
        foreach (var idAndNode in IdToNode)
        {
            idAndNode.Value.AppendEntriesRPC(NodeId, CurrentTerm);
        }
    }
    public void SendVotes()
    {
        foreach (var idAndNode in IdToNode)
        {
            idAndNode.Value.RequestVoteRPC(NodeId, CurrentTerm);
        }
    }

    public void ResponseRequestVoteRPC(long serverNodeId, bool wasVoteGiven)
    {
        int votesNeededToWinTheElection = (IdToNode.Count / 2) + 1;

        if (IdToVotedForMe.ContainsKey(serverNodeId))
        {
            IdToVotedForMe[serverNodeId] = wasVoteGiven;
            int notesThatveVotedForMe = IdToVotedForMe.Where(x => x.Value == true).Count();
            if (notesThatveVotedForMe >= votesNeededToWinTheElection) TransitionToLeader();
        }
        else
        {
            throw new Exception("This serverNode wasn't passed in as a neighbor at initialization");
        }
    }
    public void RequestVoteRPC(long senderId, int senderTerm)
    {
        if(State == ServerState.Paused) return;

        IServerNode nodeRequestingVote = IdToNode[senderId];

        nodeRequestingVote.ResponseRequestVoteRPC(NodeId, true);
    }

    public void TransitionToPaused()
    {
        State = ServerState.Paused;
        ElectionTimer.Stop();
        if(HeartbeatTimer != null) HeartbeatTimer.Stop();
    }
}