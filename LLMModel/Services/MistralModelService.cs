using System.Linq;
using LLMModel.Services.UseCases;

namespace LLMModel;

using Mistral.SDK;
using Mistral.SDK.DTOs;
using ChatMessage = Mistral.SDK.DTOs.ChatMessage;

public class MistralModelService
{
    private readonly Dictionary<string, List<ChatMessage>> _chatHistories = new();
    private readonly IReadOnlyList<IMistralUseCase> _useCases;
    private readonly string _apiKey;

    public MistralModelService(string apiKey)
    {
        _apiKey = apiKey;
        _useCases = new List<IMistralUseCase>
        {
            new WordDefinitionUseCase(),
            new CalendarEventUseCase()
        };
        foreach (var useCase in _useCases)
        {
            _chatHistories[useCase.Name] = new List<ChatMessage> { useCase.SystemMessage };
        }
    }

    public async Task<string> GetResponse(string input)
    {
        try
        {
            var client = GetLlM();
            if (client == null)
            {
                throw new SystemException("Failed to create Mistral client");
            }

            var useCase = _useCases.FirstOrDefault(candidate => candidate.CanHandle(input)) ?? _useCases.First();
            var chatHistory = _chatHistories[useCase.Name];
            chatHistory.Add(new ChatMessage(ChatMessage.RoleEnum.User, input));
            var request = new ChatCompletionRequest(ModelDefinitions.MistralSmall, chatHistory,
                responseFormat: useCase.ResponseFormat);
            var response = await client.Completions.GetCompletionAsync(request);
            var output = response.Choices.First().Message.Content;
            if (string.IsNullOrEmpty(output))
            {
                throw new SystemException("Model output is null/empty for input: " + input);
            }
            var formattedResponse = useCase.ProcessOutput(output);
            Console.WriteLine("MistralModelService: Processed use case " + useCase.Name);
            chatHistory.Add(new ChatMessage(ChatMessage.RoleEnum.Assistant, output));
            return formattedResponse;
        }
        catch (Exception e)
        {
            Console.WriteLine("MistralModelService: " + e.Message);
            return null;
        }
    }

    private MistralClient? GetLlM()
    {
        try
        {
            return new MistralClient(apiKeys: _apiKey);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
            return null;
        }
    }
}
