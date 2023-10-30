using Grpc.Core;
using Grpc.Net.Client;
using GrpcService1;
using System.Collections.Concurrent;
using System.IO;

namespace GrpcService1.Services
{
   public class GreeterService : Greeter.GreeterBase
   {
      private readonly ILogger<GreeterService> _logger;
      private static ConcurrentDictionary<string, IServerStreamWriter<HelloReply>> clients = new ConcurrentDictionary<string, IServerStreamWriter<HelloReply>>();


      public GreeterService(ILogger<GreeterService> logger)
      {
         _logger = logger;
      }

      public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
      {
         return Task.FromResult(new HelloReply
         {
            Message = "Hello " + request.Name
         });
      }

      public override async Task SayHelloStream(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
      {
         while (true)
         {
            var clientMessage = await ReadMessageWithTimeoutAsync(requestStream, Timeout.InfiniteTimeSpan);
            Console.WriteLine("New message");

            switch (clientMessage.ContentCase)
            {
               case HelloRequest.ContentOneofCase.Name:
                  await AddClient(new ChatClient
                  {
                     StreamWriter = responseStream,
                     UserName = clientMessage.Name
                  });

                  await SendBroadcastMessage(DateTime.UtcNow + " " + clientMessage.Name + " joined");
                  break;

                  
               case HelloRequest.ContentOneofCase.Message:
                  await SendBroadcastMessage(clientMessage.Message);
                  break;
            }

         }
         //await foreach(var req in requestStream.ReadAllAsync())
         //{
         //   var message = new HelloReply { Message = "Hello " + req.Name + " " + DateTime.UtcNow };
         //   var tasks = new List<Task>();

         //   foreach (var client in clients)
         //   {
         //      if (client != null && client != default)
         //      {
         //         tasks.Add(client.StreamWriter.WriteAsync(message));
         //      }
         //   }

         //   await Task.WhenAll(tasks);
         //}
      }

      public async Task SendBroadcastMessage(string messageBody)
      {
         var message = new HelloReply { Message = messageBody };
         var tasks = new List<Task>() { };

         foreach (KeyValuePair<string, IServerStreamWriter<HelloReply>> client in clients)
         {
            tasks.Add(client.Value.WriteAsync(message));
         }

         await Task.WhenAll(tasks);
      }

      public async Task AddClient(ChatClient chatClient)
      {
         var existingUser = clients.FirstOrDefault(c => c.Key == chatClient.UserName);
         if (existingUser.Key == null)
         {
            clients.TryAdd(chatClient.UserName, chatClient.StreamWriter);
         }

         await Task.CompletedTask;
      }

      public async Task<HelloRequest> ReadMessageWithTimeoutAsync(IAsyncStreamReader<HelloRequest> requestStream, TimeSpan timeout)
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

    public class ChatClient
    {
        public IServerStreamWriter<HelloReply> StreamWriter { get; set; }
        public string UserName { get; set; }
   }
 }