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
            BootstrapServers = _configuration.Env.Kafka.BootstrapServers
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }
    
    public async Task ProduceAsync(MessageRequest request)
    {
        try
        {
            string message = JsonConvert.SerializeObject(request);
            var kafkaMessage = new Message<Null, string>() { Value = message };
        
            var deliveryResult = await _producer.ProduceAsync(
                _configuration.Env.Kafka.ProducerTopic, 
                kafkaMessage
            );
        
            Console.WriteLine($"[ProducerService] Message delivered to partition {deliveryResult.Partition} at topic {deliveryResult.Topic}");
        }
        catch (ProduceException<Null, string> ex)
        {
            Console.WriteLine($"[ProducerService] Failed to produce message: {ex.Error.Reason}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProducerService] Unexpected error: {ex.Message}");
            throw;
        }
    }
    
    public async Task ProduceAsync(WordEntity request)
    {
        try
        {
            string message = JsonConvert.SerializeObject(request);
            var kafkaMessage = new Message<Null, string>() { Value = message };
        
            var deliveryResult = await _producer.ProduceAsync(
                _configuration.Env.Wordgame.ProducerTopic, 
                kafkaMessage
            );
        
            Console.WriteLine($"[ProducerService] Message delivered to partition {deliveryResult.Partition} at topic {deliveryResult.Topic}");
        }
        catch (ProduceException<Null, string> ex)
        {
            Console.WriteLine($"[ProducerService] Failed to produce message: {ex.Error.Reason}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProducerService] Unexpected error: {ex.Message}");
            throw;
        }
    }

}