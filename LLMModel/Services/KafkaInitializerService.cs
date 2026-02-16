using Confluent.Kafka;
using Confluent.Kafka.Admin;
using LLMModel;

namespace TeleBot.Services;

public class KafkaInitializerService(EnvSettings envSettings)
{
    private EnvSettings _envSettings = envSettings;

    public async Task init()
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = _envSettings.Env.Kafka.BootstrapServers
        };
        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        try
        {
            // Get existing topics
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();

            // Define topics to create
            var topicsToCreate = new List<string>
            {
                _envSettings.Env.Kafka.ConsumerTopic,
                _envSettings.Env.Kafka.ProducerTopic,
                _envSettings.Env.Wordgame.ConsumerTopic,
                _envSettings.Env.Wordgame.ProducerTopic
            };

            var topicSpecifications = topicsToCreate
                .Where(topic => !existingTopics.Contains(topic))
                .Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                })
                .ToList();

            if (topicSpecifications.Any())
            {
                Console.WriteLine($"Creating {topicSpecifications.Count} topic(s)...");
                
                await adminClient.CreateTopicsAsync(topicSpecifications);
                
                foreach (var spec in topicSpecifications)
                {
                    Console.WriteLine($"✓ Topic '{spec.Name}' created successfully");
                }
            }
            else
            {
                Console.WriteLine("All required topics already exist");
            }
        }
        catch (CreateTopicsException e)
        {
            // Handle individual topic creation failures
            foreach (var result in e.Results)
            {
                if (result.Error.Code != ErrorCode.TopicAlreadyExists)
                {
                    Console.WriteLine($"✗ Failed to create topic '{result.Topic}': {result.Error.Reason}");
                }
                else
                {
                    Console.WriteLine($"Topic '{result.Topic}' already exists");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error ensuring topics exist: {ex.Message}");
            throw;
        }
    }
}