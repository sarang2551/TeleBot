using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Telegram.Bot;

namespace TeleBot.Services;

public abstract class ConsumerService<T> : BackgroundService where T : class
{
    protected readonly EnvSettings _configuration;
    protected readonly TelegramBotClient _botClient;

    private readonly IConsumer<Null, string> _consumer;

    protected abstract string TopicName { get; }
    protected abstract string GroupId { get; }

    protected ConsumerService(EnvSettings configuration, TelegramBotClient botClient)
    {
        _configuration = configuration;
        _botClient = botClient;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _configuration.Env.Kafka.BootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<Null, string>(consumerConfig).Build();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"[{GetType().Name}] Started at {DateTime.Now}");
        return StartConsuming(stoppingToken);
    }

    public async Task StartConsuming(CancellationToken token)
    {
        try
        {
            _consumer.Subscribe(TopicName);
            Console.WriteLine($"[{GetType().Name}] Subscribed to topic {TopicName}");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var message = _consumer.Consume(token);
                    if (message == null)
                    {
                        Console.WriteLine($"[{GetType().Name}] received null message");
                        continue;
                    }

                    Console.WriteLine($"[{GetType().Name}] Consumed message from topic {message.Topic}");
                    var deserializedMessage = DeserializeMessage(message.Message.Value);
                    await ProcessMessage(deserializedMessage, token);
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($"[{GetType().Name}] Consume error: {e.Error.Reason}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{GetType().Name}] Error processing message: {e.Message}");
                    if (e.InnerException != null)
                    {
                        Console.WriteLine($"[{GetType().Name}] Inner exception: {e.InnerException.Message}");
                    }
                }
            }
        }
        finally
        {
            Console.WriteLine($"[{GetType().Name}] Stopped at {DateTime.Now}");
            _consumer.Close();
        }
    }

    protected virtual T DeserializeMessage(string messageValue)
    {
        var message = JsonConvert.DeserializeObject<T>(messageValue);
        if (message == null)
        {
            throw new Exception($"[{GetType().Name}] Deserialized null message");
        }

        return message;
    }

    protected abstract Task ProcessMessage(T message, CancellationToken token);
}
