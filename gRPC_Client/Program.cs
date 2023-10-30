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
         switch (res.Message)
         {
            case "W":
            case "w":
               {
                  Console.WriteLine("Moved up");
                  break;
               }
            case "S":
            case "s":
               {
                  Console.WriteLine("Moved down");
                  break;
               }
            case "A":
            case "a":
               {
                  Console.WriteLine("Moved left");
                  break;
               }
            case "D":
            case "d":
               {
                  Console.WriteLine("Moved right");
                  break;
               }
            default:
               {
                  Console.WriteLine("Unknown?");
                  break;
               }
         }
      }
   }
});

var send = Task.Run(async () =>
{
   while(true)
   {
      var result = Console.ReadKey();
      await call.RequestStream.WriteAsync(new HelloRequest() { Message = result.Key.ToString() });
   }
});
await send;
await recieve;

Console.ReadKey();