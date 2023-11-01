using Grpc.Core;
using Grpc.Net.Client;
using GrpcService1;
using System.Collections.Concurrent;
using System.IO;

namespace GrpcService1.Services;

public class GreeterService : Greeter.GreeterBase
{
   private readonly ILogger<GreeterService> _logger;
   private static ConcurrentDictionary<string, IServerStreamWriter<ActionReply>> clients = new();


   public GreeterService(ILogger<GreeterService> logger)
   {
      _logger = logger;
   }

   //// Вроде умер.
   //public override Task<ActionReply> SayHello(ActionRequest request, ServerCallContext context)
   //{
   //   return Task.FromResult(new ActionReply
   //   {
   //      Answer = "Hello " + request.Name
   //   });
   //}

   public override async Task ActionStream(IAsyncStreamReader<ActionRequest> requestStream, IServerStreamWriter<ActionReply> responseStream, ServerCallContext context)
   {
      while (true)
      {
         var clientMessage = await ReadMessageWithTimeoutAsync(requestStream, Timeout.InfiniteTimeSpan);
         Console.WriteLine($"New action from ");

         switch (clientMessage.ContentCase)
         {
            case ActionRequest.ContentOneofCase.Name:
               await AddClient(new ChatClient
               {
                  StreamWriter = responseStream,
                  UserName = clientMessage.Name
               });

               await SendBroadcastMessage($"{DateTime.UtcNow} {clientMessage.Name} joined");
               break; 
            case ActionRequest.ContentOneofCase.Motion:
               string move;
               switch(clientMessage.Motion)
               {
                  case "W":
                  case "w":
                     {
                        // Some actions to move up.
                        move = "moved up";
                        break;
                     }
                  case "S":
                  case "s":
                     {
                        // Some actions to move down.
                        move = "moved down";
                        break;
                     }
                  case "A":
                  case "a":
                     {
                        // Some actions to move left.
                        move = "moved left";
                        break;
                     }
                  case "D":
                  case "d":
                     {
                        // Some actions to move right.
                        move = "moved right";
                        break;
                     }
                  case "F":
                  case "f":
                     {
                        // Some actions to act.
                        move = "acted";
                        break;
                     }
                  default:
                     {
                        // Just ignore.
                        move = " done something unknown";
                        break;
                     }
               }

               await SendBroadcastMessage($"{1} {move}");
               break;
         }
      }
   }

   private async Task SendBroadcastMessage(string messageBody)
   {
      // А зачем посредник?
      var message = new ActionReply { Answer = messageBody };
      var tasks = new List<Task>() { };

      foreach (KeyValuePair<string, IServerStreamWriter<ActionReply>> client in clients)
      {
         tasks.Add(client.Value.WriteAsync(message));
      }

      await Task.WhenAll(tasks);
   }

   private async Task AddClient(ChatClient chatClient)
   {
      var existingUser = clients.FirstOrDefault(c => c.Key == chatClient.UserName);
      if (existingUser.Key == null)
      {
         clients.TryAdd(chatClient.UserName ?? "Unexpected user", chatClient.StreamWriter);
      }

      await Task.CompletedTask;
   }

   public async Task<ActionRequest> ReadMessageWithTimeoutAsync(IAsyncStreamReader<ActionRequest> requestStream, TimeSpan timeout)
   {
      CancellationTokenSource cancellationTokenSource = new();

      cancellationTokenSource.CancelAfter(timeout);

      try
      {
         bool moveNext = await requestStream.MoveNext(cancellationTokenSource.Token);

         if (moveNext == false)
         {
            throw new Exception("connection dropped exception");
         }

         return requestStream.Current;
      }
      catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
      {
         throw new TimeoutException();
      }
   }
}

public record ChatClient
{
   public IServerStreamWriter<ActionReply>? StreamWriter { get; set; }
   public string? UserName { get; set; }
}