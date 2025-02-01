using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using TeleBot.Model;
using Telegram.Bot;

namespace TeleBot.Services;

public class ConsumerService : BackgroundService
{
        private readonly EnvSettings _configuration;
        private IConsumer<Null,MessageRequest> _consumer;
        private TelegramBotClient _botClient;

        public ConsumerService(EnvSettings configuration, TelegramBotClient botClient)
        {
            _configuration = configuration;
            _botClient = botClient;
            var consumerConfig = new ConsumerConfig()
            {
                BootstrapServers = _configuration.Env.Kafka.BootstrapServers,
                GroupId = "tele-message-topic",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            _consumer = new ConsumerBuilder<Null, MessageRequest>(consumerConfig).SetValueDeserializer(new MessageRequestDeserializer()).Build();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return StartConsuming(stoppingToken);
        }


        /** Consumes a MessageRequest type message from the TeleBot service */
    public async Task StartConsuming(CancellationToken token)
    {
        try
        {
            _consumer.Subscribe(_configuration.Env.Kafka.ConsumerTopic);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var message = _consumer.Consume(token);
                    if (message == null)
                    {
                        Console.WriteLine("received null message");
                    }
                    else
                    {
                        await _botClient.SendMessage(message.Message.Value.chat_id, message.Message.Value.content, cancellationToken:token);
                    }
                   
                }
                catch (Exception e)
                {
                    // Print the actual exception message, not just stack trace
                    Console.WriteLine($"Error processing message: {e.Message}");
                    Console.WriteLine($"Stack trace: {e.StackTrace}");
    
                    // If it's an inner exception, print that too
                    if (e.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {e.InnerException.Message}");
                    }
                }            
            }
        }
        finally
        {
            _consumer.Close();
        }
    }
}