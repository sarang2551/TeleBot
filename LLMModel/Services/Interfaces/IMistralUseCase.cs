using Mistral.SDK.DTOs;

namespace LLMModel.Services.UseCases;

public interface IMistralUseCase
{
    UseCases Name { get; }
    ChatMessage SystemMessage { get; }
    ResponseFormat ResponseFormat { get; }
    string ProcessOutput(string output);
}
