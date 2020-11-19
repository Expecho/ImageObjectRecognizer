using System.Diagnostics;
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

                  if (Debugger.IsAttached)
                  {
                      config.AddUserSecrets<Program>();
                  }
              })
              .ConfigureServices((hostingContext, services) =>
              {
                  services.AddOptions();
                  services.Configure<Configuration>(hostingContext.Configuration);

                  services.AddSingleton<IResultWriter, FileWriter>();
                  services.AddSingletonUsingTypeString<IHostedService>(hostingContext.Configuration["Implementation"]);
              })
              .ConfigureLogging((hostingContext, logging) =>
              {
                  logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                  logging.AddConsole();
                  logging.AddDebug();
                  logging.AddApplicationInsights(hostingContext.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
              });

            await builder.RunConsoleAsync();
        }
    }
}