using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.ServiceBus;
using System.Text;

namespace PerformanceMessageSender
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(configure =>
                {
                    configure
                        .AddJsonFile("appsettings.json")
                        .AddUserSecrets("d81e497b-1130-4d4d-a03c-77656bc05ada")
                        .AddCommandLine(args);
                })
                .ConfigureLogging(configure => configure.AddConsole())
                .Build();

            var logger = host.Services.GetService<ILogger<Program>>();
            var config = host.Services.GetService<IConfiguration>();

            logger.LogInformation("Sending a message to the Sales Messages queue...");
            try
            {
                var cs = config.GetConnectionString(config["ServiceBus:ConnectionStringName"]);
                var name = config["ServiceBus:TopicName"];
                var client = new TopicClient(cs, name);

                var messageBody = $"Total sales for Brazil in August: $13m.";
                var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                logger.LogInformation("Sending message: {@Message}", messageBody);

                await client.SendAsync(message);
                await client.CloseAsync();
            }
            catch (Exception error)
            {
                logger.LogError(error, error.Message);
                return;
            }
            logger.LogInformation("Sent message successfully.");
        }
    }
}
