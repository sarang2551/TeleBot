namespace LLMModel.Model;

public class WordEntity : BaseKafkaEntity<WordEntity>
{
    private int _id;
    public string definition = string.Empty;
    public string example = string.Empty;
    public string word = string.Empty;
    public int difficulty;

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
