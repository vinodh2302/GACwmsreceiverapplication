using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using wmsreceiverapplication;

namespace wmsreceiverapplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Register workers for different queues
            builder.Services.AddHostedService<ProductProcessingWorker>();
            builder.Services.AddHostedService<CustomerProcessingWorker>();
            builder.Services.AddHostedService<OrderProcessingWorker>();

            var host = builder.Build();
            host.Run();
        }
    }
}
