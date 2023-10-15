using Grpc.Core;
using Grpc.Core.Interceptors;

namespace GrpcService1.Interceptors;

public class LoggingInterceptor : Interceptor
{
   private ILogger<LoggingInterceptor> logger;

   public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
   {
      this.logger = logger;
   }

   public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
   {
      logger.Log(LogLevel.Information, $"Start: {context.Method}");
   
      await base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);

      logger.Log(LogLevel.Information, $"Finish: {context.Method}");
   }

}

