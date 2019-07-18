using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceMessageReceiver
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(configure =>
                {
                    configure
                        .AddJsonFile("appsettings.json")
                        .AddUserSecrets("c1804010-ee78-4f49-acdc-9830c502fd8a")
                        .AddCommandLine(args);
                })
                .ConfigureLogging(configure => configure.AddConsole())
                .Build();

            var logger = host.Services.GetService<ILogger<Program>>();
            var config = host.Services.GetService<IConfiguration>();

            logger.LogInformation("Receiving message from the Sales Performance Messages topic...");
            try
            {
                var cs = config.GetConnectionString(config["ServiceBus:ConnectionStringName"]);
                var name = config["ServiceBus:TopicName"];
                var subname = config["Servicebus:SubscriptionName"];
                var client = new TopicClient(cs, name);

                var subscription = new SubscriptionClient(cs, name, subname);

                var messageHandlerOptions = new MessageHandlerOptions(e => ExceptionReceivedHandler(e, logger))
                {
                    MaxConcurrentCalls = 1,
                    AutoComplete = false
                };

                subscription.RegisterMessageHandler((m, ct) => ProcessMessagesAsync(m, subscription, logger), messageHandlerOptions);

                Console.ReadLine();

                await client.CloseAsync();
            }
            catch (Exception error)
            {
                logger.LogError(error, error.Message);
                return;
            }
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs, ILogger<Program> logger)
        {
            logger.LogError(exceptionReceivedEventArgs.Exception, "Message handler encountered an exception. Context: {@Context}", exceptionReceivedEventArgs.ExceptionReceivedContext);
            return Task.CompletedTask;
        }

        static async Task ProcessMessagesAsync(Message message, SubscriptionClient client, ILogger<Program> logger)
        {
            logger.LogInformation("Received sales performance message: SequenceNumber: {@SequenceNumber} Body:{@Body}",
                message.SystemProperties.SequenceNumber,
                Encoding.UTF8.GetString(message.Body));

            await client.CompleteAsync(message.SystemProperties.LockToken);
        }
    }
}
