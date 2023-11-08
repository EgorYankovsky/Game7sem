using GrpcService1.Services;
using System.Net;


var Host = Dns.GetHostName();
var IP = Dns.GetHostAddresses(Host);
Console.WriteLine("Internal or external host? [I]\\[E]: ");
var ans = Console.ReadLine();
string ipAddress = "";
switch (ans)
{
   case "I" or "i":
      ipAddress = IP.Length == 5 ? $"{IP[4]}:7106" : $"{IP[1]}:7106";
      break;
   case "E" or "e":
      ipAddress = IP.Length == 5 ? $"{IP[3]}:7106" : $"{IP[1]}:7106";
      break;
   default:
      throw new Exception("Unexpected answer. Emergency shutdown.");
}

Console.WriteLine($"Server is running on: {ipAddress}");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Server started.");
app.Run($"http://{ipAddress}");