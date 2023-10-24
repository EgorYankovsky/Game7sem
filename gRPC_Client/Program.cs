using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService1;

// The port number must match the port of the gRPC server.

using var channel = GrpcChannel.ForAddress("https://localhost:7106");
var client = new Greeter.GreeterClient(channel);

using var call = client.SayHelloStream();

Console.WriteLine("Please, enter your name...");
var name = Console.ReadLine();
await call.RequestStream.WriteAsync(new HelloRequest() { Name = name });

var recieve = Task.Run(async () =>
{
   while (true)
   {
      await foreach (var res in call.ResponseStream.ReadAllAsync())
      {
         Console.WriteLine(res.Message);
      }
   }
});

var send = Task.Run(async () =>
{
   while(true)
   {
      var result = Console.ReadLine();

      if (string.IsNullOrEmpty(result))
      { 
         break; 
      }

      await call.RequestStream.WriteAsync(new HelloRequest() { Message = result });
   }

   await call.RequestStream.CompleteAsync();
});
await send;
await recieve;

Console.ReadKey();