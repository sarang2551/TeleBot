using Confluent.Kafka;
using LLMModel.Model;

namespace LLMModel.Services;

public class CalendarConsumerService : ConsumerService<MessageRequest>
{

    private readonly MistralModelService _mistralModelService;

    protected override string TopicName => _configuration.Env.Kafka.ConsumerTopic;
    protected override string GroupId => "calendar-consumer-group";

    public CalendarConsumerService(EnvSettings configuration, ProducerService<MessageRequest> producer) 
        : base(configuration, producer)
    {
        _mistralModelService = new MistralModelService(configuration.Env.MISTRAL_API_KEY);
    }

    protected override async Task ProcessMessage(MessageRequest message)
    {
        Console.WriteLine($"[CalendarConsumer] Processing calendar message with content: {message.content}");

        object modelResponse = await _mistralModelService.GetResponse(message.content,UseCases.UseCases.CALENDAR_EVENT);
        Console.WriteLine($"[CalendarConsumer] Response from LLM: {modelResponse}");

        var response = new MessageRequest
        {
            content = modelResponse.ToString()!,
            message_id = message.message_id,
            chat_id = message.chat_id
        };

        await _producer.ProduceAsync(response,_configuration.Env.Kafka.ProducerTopic);
    }
}