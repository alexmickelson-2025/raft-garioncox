// using raft_garioncox.Records;

// public class HttpRpcNode : INode
// {
//     public int Id { get; }
//     public string Url { get; }
//     private HttpClient client = new();

//     public HttpRpcNode(int id, string url)
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

//     public async Task RequestVoteRPC(VoteRequestDTO request)
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

//     public async Task RespondAppendEntries(RespondEntriesDTO response)
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

//     public async Task RespondVote(VoteResponseDTO response)
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
// }