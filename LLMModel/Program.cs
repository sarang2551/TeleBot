using LLMModel;
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

var producer = new ProducerService(config);
var consumer = new ConsumerService(config,producer);
// background thread task
var consumerTask = Task.Run(async () => await consumer.StartConsuming(CancellationToken.None));

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
    await consumerTask;
}
catch (OperationCanceledException)
{
    Console.WriteLine("[LLM Model Service] Consumer stopped");
}

Console.WriteLine("[LLM Model Service] Application stopped");