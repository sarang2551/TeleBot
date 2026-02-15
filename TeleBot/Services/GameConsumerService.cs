using TeleBot.Model;
using Telegram.Bot;

namespace TeleBot.Services;

public class GameConsumerService : ConsumerService<MessageRequest>
{
    protected override string TopicName => _configuration.Env.Wordgame.ConsumerTopic;
    protected override string GroupId => "tele-game-consumer-group";

    public GameConsumerService(EnvSettings configuration, TelegramBotClient botClient)
        : base(configuration, botClient)
    {
    }

    protected override async Task ProcessMessage(MessageRequest message, CancellationToken token)
    {
        Console.WriteLine($"[GameConsumerService] Consumed game message content {message.content}");
        await _botClient.SendMessage(message.chat_id, message.content, cancellationToken: token);
    }
}
