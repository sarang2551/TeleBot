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

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(config.Env.BOT_TOKEN!,cancellationToken:cts.Token);
var me = await bot.GetMe();

var producerService = new ProducerService(config);
var consumerService = new ConsumerService(config,bot);
// Fire and forget this task in the background thread
_ = Task.Run(async () => await consumerService.StartAsync(CancellationToken.None));
// Blocking task requires cancellation token to exit main thread
bot.OnMessage += CustomOnMessageHandler;
Console.WriteLine($"Started bot {me.Username}... press Enter to stop");
Console.ReadLine();
Console.WriteLine("Stopping...");

cts.Cancel();

async Task CustomOnMessageHandler(Message message,UpdateType updateType)
{
    if (message.Text == null) return;
    Console.WriteLine($"Received message: {message.Text} from user: {message.From?.Username}");
    
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
            Console.WriteLine($"Received calendar event request: {naturalLanguageMessage}");
            MessageRequest request = new MessageRequest
            {
                content = naturalLanguageMessage,
                message_id = message.MessageId.ToString(),
                chat_id = message.Chat.Id
            };
            await producerService.ProduceAsync(config.Env.Kafka.ProducerTopic, request);
            break;
        default:
            await bot.SendMessage(message.Chat, $"Command {command} not recognized");
            break;
    }
}