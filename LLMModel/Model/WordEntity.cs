using System.Text.Json.Serialization;
using LLMModel.Model.Interfaces;

namespace LLMModel.Model;

public class WordEntity : BaseKafkaEntity<WordEntity>, ITeleMessage
{
    [JsonInclude]
    public string definition = string.Empty;
    [JsonInclude]
    public string example = string.Empty;
    [JsonInclude]
    public string word = string.Empty;

    [JsonInclude] public int difficulty = 0;

    [JsonInclude] public long chat_id { get; set; } = 0;
    [JsonInclude] public int message_id { get; set; } = 0;
    
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(word) &&
        !string.IsNullOrWhiteSpace(definition) &&
        !string.IsNullOrWhiteSpace(example);

    public static string JsonSchema()
    {
        const string jsonSchema = @"
            {
              ""type"": ""object"",
              ""properties"": {
                ""word"": { ""type"": [""string"", ""null""] },
                ""definition"": { ""type"": [""string"", ""null""] },
                ""example"": { ""type"": [""string"", ""null""] },
                ""difficulty"": { ""type"": [""integer"", ""null""] },
                ""error"": { ""type"": [""string"", ""null""] }
              },
              ""required"": [""word"", ""definition"", ""example""]
            }";
        return jsonSchema;
    }
}
