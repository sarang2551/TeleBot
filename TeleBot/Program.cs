// See https://aka.ms/new-console-template for more information
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using TeleBot;
using TeleBot.Model;
using TeleBot.Services;

var jsonText = File.ReadAllText("appsettings.json");
var config = JsonConvert.DeserializeObject<EnvSettings>(jsonText);
if (config == null)
{
    Console.WriteLine("Failed to load config");
    return;
}

config.Env.Kafka.BootstrapServers =
    Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVER") ?? config.Env.Kafka.BootstrapServers;

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(config.Env.BOT_TOKEN,cancellationToken:cts.Token);
var me = await bot.GetMe();

var producerService = new ProducerService(config);
var consumerService = new ConsumerService(config,bot);
// Fire and forget this task in the background thread
_ = Task.Run(async () => await consumerService.StartAsync(CancellationToken.None));
// Blocking task requires cancellation token to exit main thread
bot.OnMessage += CustomOnMessageHandler;
Console.WriteLine($"[TeleBot Service] Started bot {me.Username} at {DateTime.Now}");

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine($"[TeleBot Service] Stopped at {DateTime.Now}");
}

cts.Cancel();

async Task CustomOnMessageHandler(Message message,UpdateType updateType)
{
    if (message.Text == null) return;
    Console.WriteLine($"[TeleBot Service] Received message: {message.Text} from user: {message.From?.Username}");
    
    var handlers = new Dictionary<string, Func<Message, string?, Task>>(StringComparer.OrdinalIgnoreCase)
    {
        ["/start"] = async (msg, _) =>
        {
            var response = "This is HotDog bot. Type /help if you wish to know my true powers";
            await bot.SendMessage(msg.Chat, response);
        },
        ["/help"] = async (msg, _) =>
        {
            var response = "Here are the available commands:\n" +
                           "/start - Start the bot\n" +
                           "/help - You used this so you know what happens\n" +
                           "/calendar - Create google calendar event links";
            await bot.SendMessage(msg.Chat, response);
        },
        ["/calendar"] = async (msg, args) =>
        {
            var naturalLanguageMessage = args ?? string.Empty;
            if (string.IsNullOrWhiteSpace(naturalLanguageMessage))
            {
                await bot.SendMessage(msg.Chat, "Please provide details after /calendar.");
                return;
            }

            Console.WriteLine($"[TeleBot Service] Received calendar event request: {naturalLanguageMessage}");
            MessageRequest request = new MessageRequest
            {
                content = naturalLanguageMessage,
                message_id = msg.MessageId.ToString(),
                chat_id = msg.Chat.Id
            };
            await producerService.ProduceAsync(request);
        },
        ["/word"] = (_, _) =>
        {
            // word game: provide the meaning and example --> I guess the word 
            // if I guess the word wrong then the difficulty of the word increases 
            // more difficult words appear more frequently (algorithm to implement this logic is required because simple sorting would be too repetitive)
            return Task.CompletedTask;
        },
        ["/addWord"] = (_, _) =>
        {
            // add a word using an API to get the definition and example use
            return Task.CompletedTask;
        }
    };

    if (!TryParseCommand(message, out var command, out var args))
    {
        await bot.SendMessage(message.Chat, "No command found in message.");
        return;
    }

    if (handlers.TryGetValue(command, out var handler))
    {
        await handler(message, args);
        return;
    }

    await bot.SendMessage(message.Chat, $"Command {command} not recognized");
}

static bool TryParseCommand(Message message, out string command, out string? args)
{
    command = string.Empty;
    args = null;

    if (message.Text == null)
    {
        return false;
    }

    var entity = message.Entities?.FirstOrDefault(e => e.Type == MessageEntityType.BotCommand && e.Offset == 0);
    if (entity != null)
    {
        var commandToken = message.Text.Substring(entity.Offset, entity.Length);
        command = NormalizeCommand(commandToken);
        args = message.Text.Length > entity.Length
            ? message.Text.Substring(entity.Length).TrimStart()
            : null;
        return true;
    }

    var firstToken = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    if (string.IsNullOrWhiteSpace(firstToken) || !firstToken.StartsWith('/'))
    {
        return false;
    }

    command = NormalizeCommand(firstToken);
    args = message.Text.Length > firstToken.Length
        ? message.Text.Substring(firstToken.Length).TrimStart()
        : null;
    return true;
}

static string NormalizeCommand(string commandToken)
{
    var command = commandToken.Trim();
    var mentionIndex = command.IndexOf('@');
    return mentionIndex > 0 ? command[..mentionIndex] : command;
}
