using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PrivateMessageSender
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
                        .AddUserSecrets("48079fe5-33c5-478d-9c66-fe81d5e0c398")
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
                var name = config["ServiceBus:QueueName"];
                var client = new QueueClient(cs, name);

                var messageBody = $"$10,000 order for bicycle parts from retailer Adventure Works.";
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
