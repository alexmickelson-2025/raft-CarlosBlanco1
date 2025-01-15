namespace myclasslib;

using System.Timers;

public class ServerNode : IServerNode
{
    public long _nodeId {get; set;}
    public long _leeaderNodeId {get; set;}
    public ServerState state { get; set; }
    public int _currentTerm {get; set;}
    public Timer _electionTimer {get; set;}
    public Timer _heartbeatTimer {get; set;}
    public ServerNode()
    {
        state = ServerState.Follower;
        _nodeId = DateTime.UtcNow.Ticks;
        ResetElectionTimer();
    }

    public ServerNode(int startTerm) : this()
    {
        _currentTerm = startTerm;
    }

    public void ResetElectionTimer()
    {
        Random rand = new Random();
        double randomValueFrom150To300 = rand.Next(150, 300);

        _electionTimer = new Timer(randomValueFrom150To300);
        _electionTimer.AutoReset = false;
        _electionTimer.Elapsed += (sender, e) => StartNewElection();
        _electionTimer.Start();
    }

    public void AppendEntriesRPC(IServerNode sender)
    {
        if(ResponseAppendEntriesRPC(sender.GetCurrentTerm()) == "accepted")
        {
            _electionTimer.Stop();
            _electionTimer.Start();
            _leeaderNodeId = sender.GetID();
        }
    }

    public string ResponseAppendEntriesRPC(int term)
    {
        return term < _currentTerm ? "rejected" : "accepted";
    }

    public void RequestVoteRPC()
    {
        throw new NotImplementedException();
    }

    public void VoteResponseRPC(int serverNodeId, bool wasVoteGiven)
    {
        throw new NotImplementedException();
    }

    public void StartNewElection() => _currentTerm += 1;
    public int GetCurrentTerm() => _currentTerm;
    public long GetID() => _nodeId;
    public long GetLeaderId() => _leeaderNodeId;
    public void TransitionToLeader()
    {
        _electionTimer.Stop();

        state = ServerState.Leader;

        _heartbeatTimer = new Timer(50);
        _heartbeatTimer.AutoReset = false;
        _heartbeatTimer.Elapsed += (sender, e) => SendAppendEntriesRPC();
        _heartbeatTimer.Start();

    }

    public void SendAppendEntriesRPC()
    {
    }
}