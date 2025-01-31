// using myclasslib;

// public class HttpServerNode : IServerNode
// {
//     public int Id { get; }
//     public string Url { get; }
//     public long NodeId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public long LeaderNodeId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public ServerState State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public int CurrentTerm { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public int CommitIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public System.Timers.Timer? ElectionTimer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public DateTime ElectionTimerStartedAt { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public System.Timers.Timer? HeartbeatTimer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public Dictionary<long, IServerNode> IdToNode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public Dictionary<long, bool?> IdToVotedForMe { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public Dictionary<long, int> IdToNextIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public Dictionary<long, bool?> IdToLogValidationStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//     public Dictionary<int, string> InternalStateMachine { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

//     private HttpClient client = new();

//     public HttpServerNode(int id, string url)
//     {
//         Id = id;
//         Url = url;
//     }

//     public async Task RequestAppendEntries(AppendEntriesData request)
//     {
//         try
//         {
//             await client.PostAsJsonAsync(Url + "/request/appendEntries", request);
//         }
//         catch (HttpRequestException)
//         {
//             Console.WriteLine($"node {Url} is down");
//         }
//     }

//     public async Task RequestVote(VoteRequestData request)
//     {
//         try
//         {
//             await client.PostAsJsonAsync(Url + "/request/vote", request);
//         }
//         catch (HttpRequestException)
//         {
//             Console.WriteLine($"node {Url} is down");
//         }
//     }

//     public async Task RespondAppendEntries(RespondEntriesData response)
//     {
//         try
//         {
//             await client.PostAsJsonAsync(Url + "/response/appendEntries", response);
//         }
//         catch (HttpRequestException)
//         {
//             Console.WriteLine($"node {Url} is down");
//         }
//     }

//     public async Task ResponseVote(VoteResponseData response)
//     {
//         try
//         {
//             await client.PostAsJsonAsync(Url + "/response/vote", response);
//         }
//         catch (HttpRequestException)
//         {
//             Console.WriteLine($"node {Url} is down");
//         }
//     }

//     public async Task SendCommand(ClientCommandData data)
//     {
//         await client.PostAsJsonAsync(Url + "/request/command", data);
//     }

//     public Task AppendEntriesRPC(long senderId, int senderTerm, List<LogEntry>? entries, int? entryIndex, int? highestCommitedIndex)
//     {
//         throw new NotImplementedException();
//     }

//     public Task ResponseAppendEntriesRPC(long senderId, bool isResponseRejecting, int? senderTerm, int? commitIndex)
//     {
//         throw new NotImplementedException();
//     }

//     public Task RequestVoteRPC(long senderId, int senderTerm)
//     {
//         throw new NotImplementedException();
//     }

//     public Task ResponseRequestVoteRPC(long serverNodeId, bool wasVoteGiven)
//     {
//         throw new NotImplementedException();
//     }

//     public Task SendHeartBeat()
//     {
//         throw new NotImplementedException();
//     }

//     public Task SendVotes()
//     {
//         throw new NotImplementedException();
//     }

//     public void StartNewElection()
//     {
//         throw new NotImplementedException();
//     }

//     public void StartNewElectionTimer(double electionTimeout = 0)
//     {
//         throw new NotImplementedException();
//     }

//     public Task TransitionToLeader()
//     {
//         throw new NotImplementedException();
//     }

//     public Task TransitionToCandidate()
//     {
//         throw new NotImplementedException();
//     }

//     public Task TransitionToPaused()
//     {
//         throw new NotImplementedException();
//     }

//     public Task TransitionToFollower()
//     {
//         throw new NotImplementedException();
//     }

//     public void AddNeighbors(List<IServerNode> neighbors)
//     {
//         throw new NotImplementedException();
//     }

//     public void SendCommandToLeader(LogEntry entry)
//     {
//         throw new NotImplementedException();
//     }

//     public void SendConfirmationResponseToClient()
//     {
//         throw new NotImplementedException();
//     }
// }
