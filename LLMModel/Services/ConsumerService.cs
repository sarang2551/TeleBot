using LLMModel.Model;
using Newtonsoft.Json;

namespace LLMModel.Services;

using Confluent.Kafka;

public class ConsumerService : BackgroundService
{
    private readonly EnvSettings _configuration;

    private readonly IConsumer<Null, MessageRequest> _consumer;
    
    private readonly ProducerService _producer;

    private readonly MistralModelService _mistralModelService;

    public ConsumerService(EnvSettings configuration, ProducerService producer)
    {
        _configuration = configuration;
        _producer = producer;
        _mistralModelService = new(_configuration.Env.MISTRAL_API_KEY);
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
                        Console.WriteLine("[ConsumerService] received null message");
                    }
                    else
                    {
                        Console.WriteLine("[ConsumerService] Received message from topic " + message.Topic +
                                          " processing ...");
                        string modelResponse = await _mistralModelService.GetResponse(message.Message.Value.content);
                        Console.WriteLine("[ConsumerService] Response from LLM: " + modelResponse);
                        var response = new MessageRequest
                        {
                            content = modelResponse, message_id = message.Message.Value.message_id,
                            chat_id = message.Message.Value.chat_id
                        };
                        await _producer.ProduceAsync(response);

                    }

                }
                catch (ConsumeException e)
                {
                    Console.WriteLine("[ConsumerService] Consume error: " + e.Error.Reason);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ConsumerService] Error processing message " + e.StackTrace);
                }            
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"[Consumer Service] Fatal error in consumer: {ex.Message}");
            Console.WriteLine($"[Consumer Service] StackTrace: {ex.StackTrace}");
        }
        finally
        {
            Console.WriteLine($"[ConsumerService] Stopped at {DateTime.Now}");
            _consumer.Close();
        }
    }
}