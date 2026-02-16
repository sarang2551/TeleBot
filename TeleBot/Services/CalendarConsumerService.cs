using TeleBot.Model;
using Telegram.Bot;

namespace TeleBot.Services;

public class CalendarConsumerService : ConsumerService<MessageRequest>
{
    protected override string TopicName => _configuration.Env.Kafka.ConsumerTopic;
    protected override string GroupId => "tele-calendar-consumer-group";

    public CalendarConsumerService(EnvSettings configuration, TelegramBotClient botClient)
        : base(configuration, botClient)
    {
    }

    protected override async Task ProcessMessage(MessageRequest message, CancellationToken token)
    {
        Console.WriteLine($"[CalendarConsumerService] Consumed message content {message.content}");
        await _botClient.SendMessage(message.chat_id, message.content, cancellationToken: token);
    }
}
