using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService1;

// The port number must match the port of the gRPC server.

// Как передать условно ip?
using var channel = GrpcChannel.ForAddress("https://localhost:7106");
var client = new Greeter.GreeterClient(channel);

using var call = client.SayHelloStream();

var readTask = Task.Run(async () =>
{
   await foreach (var res in call.ResponseStream.ReadAllAsync())
   {
      Console.WriteLine(res.Message);
   }
});

while(true)
{
   var result = Console.ReadLine();

   if (string.IsNullOrEmpty(result))
   { 
      break; 
   }

   await call.RequestStream.WriteAsync(new HelloRequest() { Name = result });
}

await call.RequestStream.CompleteAsync();
await readTask;

Console.ReadKey();