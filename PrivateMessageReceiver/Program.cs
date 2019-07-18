using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PrivateMessageReceiver
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
                        .AddUserSecrets("09808d2f-e9a5-4846-aa48-86603ed79d88")
                        .AddCommandLine(args);
                })
                .ConfigureLogging(configure => configure.AddConsole())
                .Build();

            var logger = host.Services.GetService<ILogger<Program>>();
            var config = host.Services.GetService<IConfiguration>();

            logger.LogInformation("Receiving message from the Sales Messages queue...");
            try
            {
                var cs = config.GetConnectionString(config["ServiceBus:ConnectionStringName"]);
                var name = config["ServiceBus:QueueName"];
                var client = new QueueClient(cs, name);

                var messageHandlerOptions = new MessageHandlerOptions(e => ExceptionReceivedHandler(e, logger))
                {
                    MaxConcurrentCalls = 1,
                    AutoComplete = false
                };

                client.RegisterMessageHandler((m, ct) => ProcessMessagesAsync(m, client, logger), messageHandlerOptions);

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

        static async Task ProcessMessagesAsync(Message message, QueueClient client, ILogger<Program> logger)
        {
            logger.LogInformation("Received message: SequenceNumber: {@SequenceNumber} Body:{@Body}",
                message.SystemProperties.SequenceNumber,
                Encoding.UTF8.GetString(message.Body));

            await client.CompleteAsync(message.SystemProperties.LockToken);
        }
    }
}
