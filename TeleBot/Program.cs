// See https://aka.ms/new-console-template for more information
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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
var PROCESSING = false;

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(config.Env.BOT_TOKEN!,cancellationToken:cts.Token);
var me = await bot.GetMe();

var producerService = new ProducerService(config);
var consumerService = new ConsumerService(config,bot);
await consumerService.StartAsync(cts.Token);
bot.OnMessage += CustomOnMessageHandler;
Console.WriteLine($"Started bot {me.Username}... press Enter to stop");
Console.ReadLine();
Console.WriteLine("Stopping...");
cts.Cancel();

async Task CustomOnMessageHandler(Message message,UpdateType updateType)
{
    if (PROCESSING)
    {
        await bot.SendMessage(message.Chat,"Still processing!");
        return;
    }
    if (message.Text == null) return;
    Console.WriteLine($"Received message: {message.Text} from user: {message.From?.Username}");
    //await bot.SendMessage(message?.Chat!, $"Bot received message: {message?.Text}");
    string response;
    switch (message.Text.Split(' ')[0].ToLower()) // Extract command and make it case insensitive
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
            var naturalLanguageMessage = message.Text.Split(" ")[1];
            Console.WriteLine($"Received calendar event request: {naturalLanguageMessage.Substring(0,20)}");
            MessageRequest request = new MessageRequest
            {
                content = naturalLanguageMessage,
                message_id = message.MessageId.ToString(),
                chat_id = message.Chat.Id
            };
            producerService.ProduceAsync(config.Env.Kafka.ProducerTopic, request).Wait();
            break;
        default:
            break;
    }
}