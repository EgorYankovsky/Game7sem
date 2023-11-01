using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService1;

// The port number must match the port of the gRPC server.
Console.WriteLine("Type localhost or required ip");
string? address;
address = Console.ReadLine();
bool isConnected = false;
while (!isConnected)
{
   using var sw = new StreamWriter("Info.txt");
   var channel = GrpcChannel.ForAddress($"https://{address}");
   sw.WriteLine($"1. {channel.State}");
   var client = new Greeter.GreeterClient(channel);
   sw.WriteLine($"2. {channel.State}");
   using var call = client.ActionStream();

   sw.WriteLine($"3. {channel.State}");
   while (channel.State == ConnectivityState.Connecting)
      continue;

   sw.WriteLine($"4. {channel.State}");
   if (channel.State == ConnectivityState.Ready)
   {
      isConnected = true;
      Console.WriteLine("Please, enter your name...");
      var name = Console.ReadLine();
      await call.RequestStream.WriteAsync(new ActionRequest() { Name = name });

      var recieve = Task.Run(async () =>
      {
         while (true)
         {
            await foreach (var res in call.ResponseStream.ReadAllAsync())
            {
               Console.WriteLine(res.Answer);
            }
         }
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
                  call.Dispose();
                  isDisposed = true;
                  break;
               default:
                  Console.WriteLine("Unknown action!");
                  break;
            }
         }
      });
      await send;
      await recieve;
   }
   else if (channel.State == ConnectivityState.TransientFailure)
   {
      Console.WriteLine("Error during connection");
      Console.WriteLine("Type localhost or required ip");
      address = Console.ReadLine();
   }
}
Console.ReadKey();