using System.Threading.Tasks;
using ImageMetadataUpdater.Services.Rx;
using ImageMetadataUpdater.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImageMetadataUpdater
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
              .ConfigureAppConfiguration((hostingContext, config) =>
              {
                  config.AddJsonFile("appsettings.json", optional: true);
                  config.AddEnvironmentVariables();

                  if (args != null)
                  {
                      config.AddCommandLine(args);
                  }
              })
              .ConfigureServices((hostContext, services) =>
              {
                  services.AddOptions();
                  services.Configure<Configuration>(hostContext.Configuration.GetSection("Configuration"));

                  var path = hostContext.Configuration.GetSection("Configuration").Get<Configuration>().Path;
                  services.AddSingleton<IResultWriter, FileWriter>();
                  services.AddSingleton<IHostedService, RxUpdater>();
              })
              .ConfigureLogging((hostingContext, logging) =>
              {
                  logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                  logging.AddConsole();
              });

            await builder.RunConsoleAsync();
        }
    }
}