using System.Threading.Tasks;
using ImageObjectRecognizer.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImageObjectRecognizer
{

    internal class Program
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

                  if (hostingContext.HostingEnvironment.IsDevelopment())
                  {
                      config.AddUserSecrets<Program>();
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
                  logging.AddDebug();
              });

            await builder.RunConsoleAsync();
        }
    }
}