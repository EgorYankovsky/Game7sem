using Grpc.Core;
using Grpc.Net.Client;
using GrpcService1;
using System.Collections.Concurrent;
using System.IO;

namespace GrpcService1.Services;

internal record ChatClient
{
   public IServerStreamWriter<ActionReply>? StreamWriter { get; set; }
   public string? UserName { get; set; }
}


public class GreeterService : Greeter.GreeterBase
{
   private static ConcurrentDictionary<string, IServerStreamWriter<ActionReply>> clients = new();

   public override async Task ActionStream(IAsyncStreamReader<ActionRequest> requestStream, IServerStreamWriter<ActionReply> responseStream, ServerCallContext context)
   {
      while (true)
      {
         var clientMessage = await ReadMessageWithTimeoutAsync(requestStream, Timeout.InfiniteTimeSpan);

         switch (clientMessage.ContentCase)
         {
            case ActionRequest.ContentOneofCase.Name:
               await AddClientAsync(new ChatClient
               {
                  StreamWriter = responseStream,
                  UserName = clientMessage.Name
               });
               Console.WriteLine($"Connected new client {clientMessage.Name}");
               await SendBroadcastMessageAsync($"{DateTime.UtcNow} {clientMessage.Name} joined");
               break;
            case ActionRequest.ContentOneofCase.Motion:
               string move = clientMessage.Motion switch
               {
                  "W" or "w" => "moved up",
                  "S" or "s" => "moved down",
                  "A" or "a" => "moved left",
                  "D" or "d" => "moved right",
                  _ => throw new NotImplementedException()
               };
               await SendBroadcastMessageAsync($"{1} {move}");
               break;
            case ActionRequest.ContentOneofCase.Force:
               await SendBroadcastMessageAsync($"{1} forced");
               break;
            case ActionRequest.ContentOneofCase.Quit:
               await RemoveClientAsync(new ChatClient
               {
                  UserName = clientMessage.Quit,
                  StreamWriter = responseStream
               });
               break;
         }
      }
   }

   private static async Task SendBroadcastMessageAsync(string messageBody)
   {
      var message = new ActionReply { Answer = messageBody };
      var tasks = new List<Task>() { };
      foreach (KeyValuePair<string, IServerStreamWriter<ActionReply>> client in clients)
         tasks.Add(client.Value.WriteAsync(message));
      await Task.WhenAll(tasks);
   }

   private static async Task AddClientAsync(ChatClient chatClient)
   {
      var existingUser = clients.FirstOrDefault(c => c.Key == chatClient.UserName);
      if (existingUser.Key == null)
         clients.TryAdd(chatClient.UserName ?? "Unexpected user", chatClient.StreamWriter);
      await Task.CompletedTask;
   }


   // Должно работать.
   private static async Task RemoveClientAsync(ChatClient chatClient)
   {
      var existingUser = clients.FirstOrDefault(c => c.Key == chatClient.UserName);
      if (existingUser.Key == null)
         Console.WriteLine("No such user");
      else
      {
         Console.WriteLine($"{existingUser.Key} left us.");
         clients.TryRemove(existingUser);
      }
      await Task.CompletedTask;
   }

   public async Task<ActionRequest> ReadMessageWithTimeoutAsync(IAsyncStreamReader<ActionRequest> requestStream, TimeSpan timeout)
   {
      CancellationTokenSource cancellationTokenSource = new();

      try
      {
         cancellationTokenSource.CancelAfter(timeout);

         bool moveNext = await requestStream.MoveNext(cancellationTokenSource.Token);

         if (moveNext == false)
         {
            throw new Exception("connection dropped exception");
         }

         return requestStream.Current;
      }
      catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
      {
         Console.WriteLine("Что это?");
         throw new TimeoutException();
      }
      catch (Exception ex)
      {
         Console.WriteLine($"Вылет где надо: {ex}");
         throw new Exception();
      }
   }
}