using GrpcService1.Interceptors;

namespace GrpcService1
{
   public class Startup
   {
      public void ConfigureServices(IServiceCollection services)
      {
         var serviceProvider = services.BuildServiceProvider();
         var logger = serviceProvider.GetService<ILogger<LoggingInterceptor>>();

         services.AddGrpc(options =>
         {
            options.Interceptors.Add(typeof(LoggingInterceptor), logger);
         });
      }
   }
}
