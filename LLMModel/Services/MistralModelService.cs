using System.Linq;
using LLMModel.Model;
using LLMModel.Services.Interfaces;
using LLMModel.Services.UseCases;

namespace LLMModel.Services;

using Mistral.SDK;
using Mistral.SDK.DTOs;
using ChatMessage = Mistral.SDK.DTOs.ChatMessage;

public class MistralModelService
{
    private readonly Dictionary<UseCases.UseCases, List<ChatMessage>> _chatHistories = new();
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

    // TODO: Refactor the MistralModelService to return custom classes from the GetResponse function instead of just a generic object
    public async Task<object> GetResponse(string input, UseCases.UseCases useCaseName)
    {
        try
        {
            var client = GetLlM();
            if (client == null)
            {
                throw new SystemException("[MistralModelService] Failed to create Mistral client");
            }

            var useCase = _useCases.FirstOrDefault(c => c.Name == useCaseName);
            if (useCase == null)
            {
                throw new SystemException("[MistralModelService] Failed to find use case for " + useCaseName);
            }
            var chatHistory = _chatHistories[useCase.Name];
            chatHistory.Add(new ChatMessage(ChatMessage.RoleEnum.User, input));
            var request = new ChatCompletionRequest(ModelDefinitions.MistralSmall, chatHistory,
                responseFormat: useCase.ResponseFormat);
            var response = await client.Completions.GetCompletionAsync(request);
            var output = response.Choices.First().Message.Content;
            if (string.IsNullOrEmpty(output))
            {
                throw new SystemException("[MistralModelService] Model output is null/empty for input: " + input);
            }
            var formattedResponse = useCase.ProcessOutput(output);
            Console.WriteLine("[MistralModelService]: Processed use case " + useCase.Name + " with output : " + formattedResponse);
            chatHistory.Add(new ChatMessage(ChatMessage.RoleEnum.Assistant, output));
            return formattedResponse;
        }
        catch (Exception e)
        {
            throw new SystemException("[MistralModelService] Error getting response " + e.Message);
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
