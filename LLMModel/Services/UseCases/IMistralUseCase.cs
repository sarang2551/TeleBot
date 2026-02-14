using Mistral.SDK.DTOs;

namespace LLMModel.Services.UseCases;

public interface IMistralUseCase
{
    string Name { get; }
    bool CanHandle(string input);
    ChatMessage SystemMessage { get; }
    ResponseFormat ResponseFormat { get; }
    string ProcessOutput(string output);
}
