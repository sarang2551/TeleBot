using Confluent.Kafka;
using Newtonsoft.Json;
using TeleBot.Model;

namespace TeleBot.Services;

public class ProducerService
{
    private IProducer<Null, string> _producer;
    private readonly EnvSettings _configuration;

    public ProducerService(EnvSettings configuration)
    {
        _configuration = configuration;
        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = _configuration.Env.Kafka.BootstrapServers // [ localhost:9092]
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }
    
    public async Task ProduceAsync(string topic, MessageRequest request)
    {
        string message = JsonConvert.SerializeObject(request);
        var kafkaMessage = new Message<Null, string>() { Value = message };
        await _producer.ProduceAsync(topic, kafkaMessage);
    }
}