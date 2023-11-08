using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService1;

// The port number must match the port of the gRPC server.
Console.WriteLine("Type localhost or required ip");
string? address;
address = Console.ReadLine();
bool isConnected = true;
while (isConnected)
{
   var channel = GrpcChannel.ForAddress($"http://{address}");
   var client = new Greeter.GreeterClient(channel);
   using var call = client.ActionStream();

   while (channel.State == ConnectivityState.Connecting)
      continue;

   if (channel.State == ConnectivityState.Ready)
   {
      Console.WriteLine("Please, enter your name...");
      var name = Console.ReadLine();
      await call.RequestStream.WriteAsync(new ActionRequest() { Name = name });

      var recieve = Task.Run(async () =>
      {
         while (isConnected)
            await foreach (var res in call.ResponseStream.ReadAllAsync())
               Console.WriteLine(res.Answer);
      });

      var send = Task.Run(async () =>
      {
         bool isDisposed = false;
         while (!isDisposed)
         {
            var button = Console.ReadKey();
            Console.WriteLine(button.KeyChar);
            switch (button.KeyChar)
            {
               case 'w' or 'W' or 's' or 'S' or 'a' or 'A' or 'd' or 'D':
                  await call.RequestStream.WriteAsync(new ActionRequest() { Motion = button.Key.ToString() });
                  break;
               case 'f' or 'F':
                  await call.RequestStream.WriteAsync(new ActionRequest() { Force = button.Key.ToString() });
                  break;
               case 'q' or 'Q':
                  await call.RequestStream.WriteAsync(new ActionRequest() { Quit = name });
                  await call.RequestStream.CompleteAsync();
                  call.Dispose();
                  isDisposed = true;
                  isConnected = false;
                  break;
               default:
                  Console.WriteLine("Unknown action!");
                  break;
            }
         }
      });
      await send;
      //if (isConnected) 
      await recieve;
   }
   else if (channel.State == ConnectivityState.TransientFailure)
   {
      Console.WriteLine("Error during connection");
      Console.WriteLine("Type localhost or required ip");
      address = Console.ReadLine();
   }
}
Console.WriteLine("Type any key to quit");
Console.ReadKey();