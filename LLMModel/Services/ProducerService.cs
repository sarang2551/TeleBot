using Confluent.Kafka;
using LLMModel.Model;

namespace LLMModel.Services;

public class ProducerService
{
    private readonly EnvSettings _configuration;

    private readonly IProducer<Null, MessageRequest> _producer;

    public ProducerService(EnvSettings configuration)
    {
        _configuration = configuration;
        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = _configuration.Env.Kafka.BootstrapServers // [ localhost:9092]
        };
        _producer = new ProducerBuilder<Null, MessageRequest>(producerConfig).SetValueSerializer(new MessageRequestSerializer()).Build();
    }
    
    /** Produces the event link for the TeleBot service to consume */
    public async Task ProduceAsync(MessageRequest message)
    {
        var kafkaMessage = new Message<Null, MessageRequest>{ Value = message };
        await _producer.ProduceAsync(_configuration.Env.Kafka.ProducerTopic, kafkaMessage);
    }
}