using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using raft_garioncox;
using raft_garioncox.Records;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");


var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");
var nodeIntervalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL_SCALAR") ?? throw new Exception("NODE_INTERVAL_SCALAR environment variable not set");

builder.Services.AddLogging();
var serviceName = "Node" + nodeId;
builder.Logging.AddOpenTelemetry(options =>
{
    options
      .SetResourceBuilder(
          ResourceBuilder
            .CreateDefault()
            .AddService(serviceName)
      )
      .AddOtlpExporter(options =>
      {
          options.Endpoint = new Uri("http://dashboard:18889");
      });
});
var app = builder.Build();

var logger = app.Services.GetService<ILogger<Program>>();
logger.LogInformation("Node ID {name}", nodeId);
logger.LogInformation("Other nodes environment config: {}", otherNodesRaw);


INode[] otherNodes = otherNodesRaw
  .Split(";")
  .Select(s => new HttpRpcNode(int.Parse(s.Split(",")[0]), s.Split(",")[1]))
  .ToArray();


logger.LogInformation("other nodes {nodes}", JsonSerializer.Serialize(otherNodes));


var node = new Node(otherNodes.ToList())
{
    Id = int.Parse(nodeId),
    // logger = app.Services.GetService<ILogger<Node>>(),
    IntervalScalar = int.Parse(nodeIntervalScalarRaw)
};

node.Run();

app.MapGet("/health", () => "healthy");

app.MapGet("/nodeData", () =>
{
    NodeDataDTO data = new(
        node.Id,
        node.LogState,
        node.ElectionTimeout,
        node.Term,
        node.CurrentLeader,
        node.CommittedLogIndex,
        node.State,
        node.IntervalScalar,
        node.Entries,
        node.IsPaused
    );

    return data;
});

app.MapPost("/request/appendEntries", async ([FromBody] AppendEntriesDTO request) =>
{
    logger.LogInformation("received append entries request {request}", request);
    logger.LogInformation("Log is currently {}", node.IsPaused ? "Paused" : "Running");
    await node.RequestAppendEntries(request);
});

app.MapPost("/request/vote", async ([FromBody] VoteRequestDTO request) =>
{
    logger.LogInformation("received vote request {request}", request);
    await node.RequestVoteRPC(request);
});

app.MapPost("/response/appendEntries", async ([FromBody] RespondEntriesDTO response) =>
{
    logger.LogInformation("received append entries response {response}", response);
    await node.RespondAppendEntries(response);
});

app.MapPost("/response/vote", ([FromBody] VoteResponseDTO response) =>
{
    logger.LogInformation("received vote response {response}", response);
    node.RespondVote(response);
});

app.MapPost("/request/command", ([FromBody] ClientCommandDTO data) =>
{
    logger.LogInformation("Receive command {command}", data.command);
    node.ReceiveCommand(data);
});

app.MapGet("/request/pause", () =>
{
    logger.LogInformation("Receive pause command for {id}", node.Id);
    node.Pause();
});

app.MapGet("/request/unpause", () =>
{
    logger.LogInformation("Receive unpause command for {id}", node.Id);
    node.Unpause();
});

app.Run();