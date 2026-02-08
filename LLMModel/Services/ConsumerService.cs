using LLMModel.Model;
using Newtonsoft.Json;

namespace LLMModel.Services;

using Confluent.Kafka;

public abstract class ConsumerService<T>: BackgroundService where T : class
{
    protected readonly EnvSettings _configuration;

    private readonly IConsumer<Null, string> _consumer;
    
    protected readonly ProducerService<T> _producer;
    
    protected abstract string TopicName { get; }
    protected abstract string GroupId { get; }

    protected ConsumerService(EnvSettings configuration, ProducerService<T> producer)
    {
        _configuration = configuration;
        _producer = producer;
        var consumerConfig = new ConsumerConfig()
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
    
    private async Task StartConsuming(CancellationToken token)
    {
        try
        {
            _consumer.Subscribe(TopicName);
            Console.WriteLine($"[{GetType().Name}] Subscribed to topic " + TopicName);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var message = _consumer.Consume(token);
                    if (message == null)
                    {
                        Console.WriteLine($"[{GetType().Name}] received null message");
                    }
                    else
                    {
                        Console.WriteLine($"[{GetType().Name}] Received message from topic " + message.Topic +
                                          " processing ...");
                        var deserializedMessage = DeserializeMessage(message.Message.Value);
                        await ProcessMessage(deserializedMessage);
                    }

                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($"[{GetType().Name}] Consume error: " + e.Error.Reason);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{GetType().Name}] Error processing message " + e.StackTrace);
                }            
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"[{GetType().Name}] Fatal error in consumer: {ex.Message}");
            Console.WriteLine($"[{GetType().Name}] StackTrace: {ex.StackTrace}");
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
        if (message == null) throw new Exception($"[{GetType().Name}] Deserialized null message");
        return message;
    }

    protected abstract Task ProcessMessage(T message);
}