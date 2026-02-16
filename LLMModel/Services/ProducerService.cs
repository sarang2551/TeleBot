using Confluent.Kafka;
using LLMModel.Model;

namespace LLMModel.Services;

public class ProducerService<TOutput>
{
    private readonly EnvSettings _configuration;

    private readonly IProducer<Null, TOutput> _producer;

    public ProducerService(EnvSettings configuration)
    {
        _configuration = configuration;
        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = _configuration.Env.Kafka.BootstrapServers
        };
        _producer = new ProducerBuilder<Null, TOutput>(producerConfig).SetValueSerializer(new BaseKafkaEntity<TOutput>()).Build();
    }
    
    /** Produces the event link for the TeleBot service to consume */
    public async Task ProduceAsync(TOutput message,string producerTopic)
    {
        var kafkaMessage = new Message<Null, TOutput>{ Value = message };
        await _producer.ProduceAsync(producerTopic, kafkaMessage);
    }
}