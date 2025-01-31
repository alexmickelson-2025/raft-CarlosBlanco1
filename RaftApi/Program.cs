// using System.Text.Json;
// using myclasslib;

// var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseUrls("http://0.0.0.0:8080");
 
 
// var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
// var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");
// var nodeIntervalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL_SCALAR") ?? throw new Exception("NODE_INTERVAL_SCALAR environment variable not set");
 
// var app = builder.Build();
 
// List<IServerNode> otherNodes = otherNodesRaw
//   .Split(";")
//   .Select(s => new HttpServerNode(int.Parse(s.Split(",")[0]), s.Split(",")[1]))
//   .ToList<IServerNode>(); 
 
// var node = new ServerNode(otherNodes)
// {
//   NodeId = int.Parse(nodeId),
// };
 
// // ServerNode.NodeIntervalScalar = double.Parse(nodeIntervalScalarRaw);
 
// // node.RunElectionLoop();
 
// app.MapGet("/health", () => "healthy");
 
// app.MapGet("/nodeData", () =>
// {
//   return new ServerNode(
//     NodeId: node.NodeId,
//     State: node.State,
//     ElectionTimeout: node.ElectionTimeout,
//     Term: node.CurrentTerm,
//     CurrentTermLeader: node.CurrentTermLeader,
//     CommittedEntryIndex: node.CommittedEntryIndex,
//     Log: node.Log,
//     State: node.State,
//     NodeIntervalScalar: RaftNode.NodeIntervalScalar
//   );
// });
 
// app.MapPost("/request/appendEntries", async (AppendEntriesData request) =>
// {
//   logger.LogInformation("received append entries request {request}", request);
//   await node.Appe(request);
// });
 
// app.MapPost("/request/vote", async (VoteRequestData request) =>
// {
//   logger.LogInformation("received vote request {request}", request);
//   await node.RequestVote(request);
// });
 
// app.MapPost("/response/appendEntries", async (RespondEntriesData response) =>
// {
//   logger.LogInformation("received append entries response {response}", response);
//   await node.RespondAppendEntries(response);
// });
 
// app.MapPost("/response/vote", async (VoteResponseData response) =>
// {
//   logger.LogInformation("received vote response {response}", response);
//   await node.ResponseVote(response);
// });
 
// app.MapPost("/request/command", async (ClientCommandData data) =>
// {
//   await node.SendCommand(data);
// });
 
// app.Run();