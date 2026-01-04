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
            Console.WriteLine($"[ConsumerService] Started at {DateTime.Now}");
            return StartConsuming(stoppingToken);
        }


        /** Consumes a MessageRequest type message from the TeleBot service */
    public async Task StartConsuming(CancellationToken token)
    {
        try
        {
            _consumer.Subscribe(_configuration.Env.Kafka.ConsumerTopic);
            Console.WriteLine("[Consumer Service] Subscribed to topic " + _configuration.Env.Kafka.ConsumerTopic);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var message = _consumer.Consume(token);
                    if (message == null)
                    {
                        Console.WriteLine("[Consumer Service] received null message");
                    }
                    else
                    {
                        Console.WriteLine($"[Consumer Service] Consumed message content {message.Message.Value.content}");
                        await _botClient.SendMessage(message.Message.Value.chat_id, message.Message.Value.content, cancellationToken:token);
                    }
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Consumer Service] Error processing message: {e.Message}");
                    
                    if (e.InnerException != null)
                    {
                        Console.WriteLine($"[Consumer Service]  Inner exception: {e.InnerException.Message}");
                    }
                }            
            }
        }
        finally
        {
            Console.WriteLine($"[Consumer Service] Stopped at {DateTime.Now}");
            _consumer.Close();
        }
    }
}