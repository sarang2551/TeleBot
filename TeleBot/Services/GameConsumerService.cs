using TeleBot.Model;
using Telegram.Bot;

namespace TeleBot.Services;

public class GameConsumerService : ConsumerService<WordEntity>
{
    protected override string TopicName => _configuration.Env.Wordgame.ConsumerTopic;
    protected override string GroupId => "tele-game-consumer-group";
    
    private FirebaseService _firebaseService;

    public GameConsumerService(EnvSettings configuration, TelegramBotClient botClient, FirebaseService firebaseService)
        : base(configuration, botClient)
    {
        _firebaseService = firebaseService;
    }

    protected override async Task ProcessMessage(WordEntity message, CancellationToken token)
    {
        Console.WriteLine($"[GameConsumerService] Consumed word message content for word: {message.word}");
        await _firebaseService.AddWord(message);
        string content = $"Saved {message.word}\n Definition: {message.definition}\n Example: {message.example}";
        await _botClient.SendMessage(message.chat_id, content, cancellationToken: token);
    }
}
