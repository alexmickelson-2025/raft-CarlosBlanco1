namespace myclasslib;

public interface IServerNode
{
    public void AppendEntriesRPC(IServerNode sender);
    public string ResponseAppendEntriesRPC(int term);
    public void RequestVoteRPC();
    public void VoteResponseRPC(int serverNodeId, bool wasVoteGiven);
    public void SendAppendEntriesRPC();
    public void StartNewElection();
    public void ResetElectionTimer();
    public void TransitionToLeader();
    public int GetCurrentTerm();
    public long GetID();
    public long GetLeaderId();

}
