using LLMModel;
using LLMModel.Model;
using LLMModel.Services;
using Newtonsoft.Json;
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

var kafkaInitializerService = new KafkaInitializerService(config);
await kafkaInitializerService.init();

var calendarProducer = new ProducerService<MessageRequest>(config);
var wordGameProducer = new ProducerService<WordEntity>(config);
var calendarConsumer = new CalendarConsumerService(config,calendarProducer);
var wordGameConsumer = new WordGameConsumerService(config, wordGameProducer);

// background thread tasks
var consumerTasks = Task.WhenAll(calendarConsumer.StartAsync(CancellationToken.None), wordGameConsumer.StartAsync(CancellationToken.None));

Console.WriteLine($"[LLM Model Service] Started at {DateTime.Now} \n");
try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("[LLM Model Service] Shutdown signal received, stopping...");
}

// Wait for consumer to finish gracefully
try
{
    await consumerTasks;
}
catch (OperationCanceledException)
{
    Console.WriteLine("[LLM Model Service] Consumer stopped");
}

Console.WriteLine("[LLM Model Service] Application stopped");