// using Microsoft.AspNetCore.SignalR;
// using RangerAi.Infrastructure.Models;
// using RangerAi.Services;
//
// namespace RangerAi.Hubs
// {
//     public class RangerHub : Hub
//     {
//         public override Task OnConnectedAsync()
//         {
//             Console.WriteLine($"[SignalR] Connected: {Context.ConnectionId}");
//             return base.OnConnectedAsync();
//         }
//
//         public override Task OnDisconnectedAsync(Exception? exception)
//         {
//             Console.WriteLine($"[SignalR] Disconnected: {Context.ConnectionId}");
//             return base.OnDisconnectedAsync(exception);
//         }
//
//         public async Task Invoke(string method, object? payload)
//         {
//             Console.WriteLine($"[SignalR] Invoke - method: {method}, payload: {payload}");
//             // handle or drop
//         }
//
//         // Called by client via connection.invoke("Message", prompt)
//         public async Task Message(string prompt)
//         {
//             var request = new Chat
//             {
//                 Query = prompt
//             };
//
//             var message = new OutboundResponse(request);
//
//             message.Response = await RequestService.Handle(message);
//
//
//             // Immediate ack back to the same "Message" handler on the client
//             await Clients.Caller.SendAsync("Message", message.Response);
//
//             // Fire-and-forget: trigger the n8n workflow, passing along this ConnectionId
//             var payload = new
//             {
//                 query = prompt,
//                 connectionId = Context.ConnectionId
//             };
//
//             //something else?
//         }
//     }
// }
