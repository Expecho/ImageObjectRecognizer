using System.Threading.Tasks;
using ImageMetadataUpdater.Services;
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

                  var configuration = hostContext.Configuration.GetSection("Configuration").Get<Configuration>();

                  services.AddSingleton<IResultWriter, FileWriter>();
                  services.AddSingletonUsingTypeString<IHostedService>(configuration.Implementation);
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