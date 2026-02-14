using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LLMModel.Model;
using Mistral.SDK.DTOs;

namespace LLMModel.Services.UseCases;

public class WordDefinitionUseCase : IMistralUseCase
{
    private static readonly Regex TriggerRegex = new(@"\b(define|definition|meaning of|what does|what's the meaning)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Name => "word-definition";

    public bool CanHandle(string input)
    {
        return TriggerRegex.IsMatch(input);
    }

    public ChatMessage SystemMessage { get; } = new(ChatMessage.RoleEnum.System, $@"You are a dictionary assistant. Your task is to extract the target word from the user's request and return a concise definition and an example sentence in JSON format.

        Given a natural language request, extract the following information:
        - word: The word being defined (required)

        And populate the following information:
        - definition: A clear, concise definition (required)
        - example: A natural example sentence using the word (required)
        - error: An explanation if the request is missing the target word (optional)

        JSON STRUCTURE:
        {WordEntity.JsonSchema()}

        Rules:
        1. Always respond ONLY with valid JSON, no additional text or explanations
        2. Use the exact word the user requested, unless it is ambiguous
        3. If the word is missing or unclear, set error with a short explanation and leave other fields null
        4. Keep the definition concise (one sentence)
        5. Make the example sentence natural and grammatically correct

        Example input: 'Define serendipity'
        Example output:
        {{
          ""word"": ""serendipity"",
          ""definition"": ""The occurrence of pleasant events by chance."",
          ""example"": ""Meeting her future cofounder by accident felt like serendipity."",
          ""error"": null
        }}

        Example input with missing info: 'Can you define it?'
        Example output:
        {{
          ""word"": null,
          ""definition"": null,
          ""example"": null,
          ""error"": ""Missing target word to define.""
        }}

        Respond ONLY with the JSON object, nothing else.");

    public ResponseFormat ResponseFormat { get; } = new()
    {
        Type = ResponseFormat.ResponseFormatEnum.JSON
    };

    public string ProcessOutput(string output)
    {
        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;
        if (root.TryGetProperty("error", out var errorElement))
        {
            var errorMessage = errorElement.GetString();
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new SystemException("Invalid definition data: " + errorMessage);
            }
        }

        var wordEntity = JsonSerializer.Deserialize<WordEntity>(output);
        if (wordEntity == null || !wordEntity.IsValid)
        {
            throw new SystemException("Invalid definition data: missing required fields.");
        }

        Console.WriteLine("WordDefinitionUseCase: Created definition for word: " + wordEntity.word);
        return FormatDefinition(wordEntity);
    }

    private static string FormatDefinition(WordEntity wordEntity)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Word: {wordEntity.word}");
        sb.AppendLine($"Definition: {wordEntity.definition}");
        sb.AppendLine($"Example: {wordEntity.example}");
        return sb.ToString().Trim();
    }
}
