using LLMModel;
using LLMModel.Model;
using LLMModel.Services;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var jsonText = File.ReadAllText("appsettings.json");
var config = JsonConvert.DeserializeObject<EnvSettings>(jsonText);
if (config == null)
{
    Console.WriteLine("Failed to load config");
    return;
}
var producer = new ProducerService(config);
var consumer = new ConsumerService(config,producer);
// Instead of tight coupling via a POST request the LLMMOdel service will be lousy coupled by consuming messages from the TeleBot service instead
await consumer.StartAsync(CancellationToken.None);
// app.MapPost("/message", async(MessageRequest request) =>
//     {
//         string response = await new MistralModelService(config?.Env.MISTRAL_API_KEY ?? "").GetResponse(request.content) ?? "";
//         if (response.Length > 0)
//         {
//             // send this to the kafka topic for the telegram bot service to consume
//             
//             return Results.Ok(response);
//         }
//         return Results.NoContent();
//     })
//     .WithName("PostMessage")
//     .WithOpenApi();

app.Run();