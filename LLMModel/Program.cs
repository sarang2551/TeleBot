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
using var cts = new CancellationTokenSource();

var kafkaInitializerService = new KafkaInitializerService(config);
await kafkaInitializerService.init();

var producer = new ProducerService(config);
var consumer = new ConsumerService(config,producer);
// Instead of tight coupling via a POST request the LLMMOdel service will be lousy coupled by consuming messages from the TeleBot service instead
var consumerTask = consumer.StartAsync(cts.Token);

Console.WriteLine("LLM Model service started. Press Enter to stop...\n");
Console.ReadLine();

Console.WriteLine("Stopping...");
cts.Cancel();

// Wait for consumer to finish gracefully
try
{
    await consumerTask;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Consumer stopped");
}

Console.WriteLine("Application stopped");

