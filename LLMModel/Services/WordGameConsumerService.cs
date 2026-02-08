using LLMModel.Model;

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
        Console.WriteLine($"[WordGameConsumer] Processing word: {wordEntity.word}");

        var response = await _mistralModelService.GetResponse(wordEntity.word);
        
        // Process the word entity
        var processedEntity = ProcessWordGameLogic(wordEntity);

        // Send response back
        await _producer.ProduceAsync(processedEntity,_configuration.Env.Wordgame.ProducerTopic);
    }

    private WordEntity ProcessWordGameLogic(WordEntity wordEntity)
    {
        
        return wordEntity;
    } 
}