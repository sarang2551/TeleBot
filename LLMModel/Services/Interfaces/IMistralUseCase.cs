using Mistral.SDK.DTOs;

namespace LLMModel.Services.Interfaces;

public interface IMistralUseCase
{
    UseCases.UseCases Name { get; }
    ChatMessage SystemMessage { get; }
    ResponseFormat ResponseFormat { get; }
    object ProcessOutput(string output);
}

public interface IMistralUseCase<T> : IMistralUseCase
{
    new T ProcessOutput(string output);
}