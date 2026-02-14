// See https://aka.ms/new-console-template for more information
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using TeleBot;
using TeleBot.Model;
using TeleBot.Services;
using System.Collections.Concurrent;

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
var firebaseService = new FirebaseService(config,producerService);
var activeWordGames = new ConcurrentDictionary<long, byte>();

var baseHandlers = new Dictionary<string, Func<Message, string?, Task>>(StringComparer.OrdinalIgnoreCase)
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
                       "/calendar - Create google calendar event links\n" +
                       "/word - Start or stop the word game";
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
    ["/word"] = async (msg, args) =>
    {
        if (activeWordGames.TryRemove(msg.Chat.Id, out _))
        {
            await bot.SendMessage(msg.Chat, "Word game ended. Use /word to start again.");
            return;
        }

        activeWordGames[msg.Chat.Id] = 1;
        await bot.SendMessage(msg.Chat,
            "Word game started. Use /addWord while the game is active. Send /word again to stop.");
    }
};

var wordGameHandlers = new Dictionary<string, Func<Message, string?, Task>>(StringComparer.OrdinalIgnoreCase)
{
    ["/addWord"] = async (msg, args) =>
    {
        var requestedWord = args ?? string.Empty;
        if (string.IsNullOrWhiteSpace(requestedWord))
        {
            await bot.SendMessage(msg.Chat, "Please provide a word after /addWord."); 
            return;
        }

        await firebaseService.AddWord(requestedWord);
        // add a word using an API to get the definition and example use
        await bot.SendMessage(msg.Chat, $"Added '{requestedWord}' to the library.");
    }
};
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

    if (!TryParseCommand(message, out var command, out var args))
    {
        if (activeWordGames.ContainsKey(message.Chat.Id))
        {
            // word game: provide the meaning and example --> I guess the word
            // if I guess the word wrong then the difficulty of the word increases
            // more difficult words appear more frequently
            await bot.SendMessage(message.Chat, "Word game input received. Keep going or use /word to stop.");
            return;
        }

        await bot.SendMessage(message.Chat, "No command found in message.");
        return;
    }

    if (baseHandlers.TryGetValue(command, out var handler))
    {
        await handler(message, args);
        return;
    }

    if (activeWordGames.ContainsKey(message.Chat.Id) && wordGameHandlers.TryGetValue(command, out var wordGameHandler))
    {
        await wordGameHandler(message, args);
        return;
    }

    if (string.Equals(command, "/addWord", StringComparison.OrdinalIgnoreCase))
    {
        await bot.SendMessage(message.Chat, "Start the word game with /word before using /addWord.");
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
