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
    
    string response;
    var command =  message.Text.Split(" ")[0];
    switch (command)
    {
        case "/start":
            response = "This is HotDog bot. Type /help if you wish to know my true powers";
            await bot.SendMessage(message.Chat,response);
            break;

        case "/help":
            response = "Here are the available commands:\n" +
                       "/start - Start the bot\n" +
                       "/help - You used this so you know what happens\n" +
                       "/calendar - Create google calendar event links";
            await bot.SendMessage(message.Chat,response);
            break;
        case "/calendar":
            var naturalLanguageMessage = message.Text.Substring(command.Length + 1);
            Console.WriteLine($"[TeleBot Service] Received calendar event request: {naturalLanguageMessage}");
            MessageRequest request = new MessageRequest
            {
                content = naturalLanguageMessage,
                message_id = message.MessageId.ToString(),
                chat_id = message.Chat.Id
            };
            await producerService.ProduceAsync(request);
            break;
        case "/word":
            // word game: provide the meaning and example --> I guess the word 
            // if I guess the word wrong then the difficulty of the word increases 
            // more difficult words appear more frequently (algorithm to implement this logic is required because simple sorting would be too repetitive)
            break;
        case "/addWord":
            // add a word using an API to get the definition and example use
            break;
        default:
            await bot.SendMessage(message.Chat, $"Command {command} not recognized");
            break;
    }
}