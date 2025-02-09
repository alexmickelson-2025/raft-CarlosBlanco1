using System.Text.Json;
using myclasslib;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");


var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");
var nodeIntervalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL_SCALAR") ?? throw new Exception("NODE_INTERVAL_SCALAR environment variable not set");

var app = builder.Build();

List<IServerNode> otherNodes = otherNodesRaw
  .Split(";")
  .Select(s => new HttpRpcOtherNode(int.Parse(s.Split(",")[0]), s.Split(",")[1]))
  .ToList<IServerNode>();

var node = new ServerNode([], nodeId: (long)int.Parse(nodeId));
node.timeoutForTimer = 4;
// ServerNode.NodeIntervalScalar = double.Parse(nodeIntervalScalarRaw);

node.AddNeighbors(otherNodes);

app.MapGet("/health", () => "healthy");

app.MapGet("/getId", () => node.NodeId);

app.MapGet("/nodeData", () =>
{
  return new NodeDataDTO
  {
    Id = node.NodeId.ToString(),
    State = node.State.ToString(),
    ElectionTimeout = node.ElectionTimer == null ? 0 : (int)node.ElectionTimer.Interval,
    Term = node.CurrentTerm,
    CurrentTermLeader = node.LeaderNodeId.ToString(),
    CommittedEntryIndex = node.CommitIndex,
    Logs = node.Logs,
    InternalStateMachine = node.InternalStateMachine,
    ElectionStartedAt = node.ElectionTimerStartedAt
    // NodeIntervalScalar= RaftNode.NodeIntervalScalar
  };
});

app.MapPost("/request/appendEntries", async (AppendEntriesDTO request) =>
{
  await node.AppendEntriesRPC(request);
});

app.MapPost("/request/vote", async (RequestVoteDTO request) =>
{
  await node.RequestVoteRPC(request);
});

app.MapPost("/response/appendEntries", async (ResponseAppendEntriesDTO response) =>
{
  await node.ResponseAppendEntriesRPC(response);
});

app.MapPost("/response/vote", async (ResponseRequestVoteDTO response) =>
{
  await node.ResponseRequestVoteRPC(response);
});

app.MapPost("/request/command", (LogEntry data) =>
{
  try
  {
    node.SendCommandToLeader(data);
    return Results.Ok(new { message = "Command received successfully" });

  }
  catch (Exception e)
  {
    return Results.BadRequest(new { message = e.Message });
  }
});

app.MapGet("/start", () => node.StartTheThing());

app.Run();

