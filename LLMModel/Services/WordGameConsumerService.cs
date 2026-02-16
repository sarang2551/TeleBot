using LLMModel.Model;
using LLMModel.Services.UseCases;
using Newtonsoft.Json;

namespace LLMModel.Services;

public class WordGameConsumerService : ConsumerService<WordEntity>
{
    protected override string TopicName => _configuration.Env.Wordgame.ConsumerTopic;
    protected override string GroupId => "wordgame-consumer-group";
    private readonly MistralModelService _mistralModelService;

    public WordGameConsumerService(EnvSettings configuration, ProducerService<WordEntity> producer) 
        : base(configuration, producer)
    {
        _mistralModelService = new MistralModelService(configuration.Env.MISTRAL_API_KEY);
    }

    protected override async Task ProcessMessage(WordEntity wordEntity)
    {
        try
        {
            Console.WriteLine($"[WordGameConsumer] Processing word: {wordEntity.word}");
            var chatId = wordEntity.chat_id;
            var messageId = wordEntity.message_id;
            var response = await _mistralModelService.GetResponse("Define: " + wordEntity.word, UseCases.UseCases.WORD_DEFINITION);
            var entity = (WordEntity)response;
            entity.chat_id = chatId;
            entity.message_id = messageId;
            // Send response back
            await _producer.ProduceAsync(entity,_configuration.Env.Wordgame.ProducerTopic);
        }catch(Exception ex)
        {
            Console.WriteLine("[WordGameConsumer] Error processing message from consumer: " + ex.Message);
        }
    }
}