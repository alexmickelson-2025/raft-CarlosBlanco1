using System.Net.Http.Json;
using myclasslib;
public class HttpRpcOtherNode : IServerNode
{
  public string Url { get; }
  public long NodeId { get; set; }
  public long LeaderNodeId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public ServerState State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public int CurrentTerm { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public int CommitIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public System.Timers.Timer? ElectionTimer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public DateTime ElectionTimerStartedAt { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public System.Timers.Timer? HeartbeatTimer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public Dictionary<long, IServerNode> IdToNode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public Dictionary<long, bool?> IdToVotedForMe { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public Dictionary<long, int> IdToNextIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public Dictionary<long, bool?> IdToLogValidationStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public Dictionary<int, string> InternalStateMachine { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  public List<LogEntry> Logs { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

  private HttpClient client = new();

  public HttpRpcOtherNode(int id, string url)
  {
    NodeId = id;
    Url = url;
  }

  public async Task AppendEntriesRPC(AppendEntriesDTO data)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/request/appendEntries", data);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }

  public async Task ResponseAppendEntriesRPC(ResponseAppendEntriesDTO data)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/response/appendEntries", data);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }

  public async Task RequestVoteRPC(RequestVoteDTO data)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/request/vote", data);
    }
    catch (HttpRequestException e)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }

  public async Task ResponseRequestVoteRPC(ResponseRequestVoteDTO data)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/response/vote", data);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }

  public Task SendHeartBeat()
  {
    throw new NotImplementedException();
  }

  public Task SendVotes()
  {
    throw new NotImplementedException();
  }

  public void StartNewElection()
  {
    throw new NotImplementedException();
  }

  public void StartNewElectionTimer(double electionTimeout = 0)
  {
    throw new NotImplementedException();
  }

  public Task TransitionToLeader()
  {
    throw new NotImplementedException();
  }

  public Task TransitionToCandidate()
  {
    throw new NotImplementedException();
  }

  public Task TransitionToPaused()
  {
    throw new NotImplementedException();
  }

  public Task TransitionToFollower()
  {
    throw new NotImplementedException();
  }

  public void AddNeighbors(List<IServerNode> neighbors)
  {
    throw new NotImplementedException();
  }

  public bool SendCommandToLeader(LogEntry entry)
  {
    client.PostAsJsonAsync(Url + "/request/command", entry);
    return true;
  }

  public void SendConfirmationResponseToClient()
  {
    throw new NotImplementedException();
  }
}
