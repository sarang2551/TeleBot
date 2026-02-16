using TeleBot.Model;

namespace TeleBot.Services;

public class GameSession
{
    private readonly GameService _gameService;
    private WordEntity? _currentWord;

    public GameSession(GameService gameService)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
    }

    public string Start()
    {
        _currentWord = _gameService.GetNextWord();
        return BuildPrompt(_currentWord);
    }

    public string EvaluateAndBuildNextPrompt(string userAnswer)
    {
        if (_currentWord == null)
        {
            _currentWord = _gameService.GetNextWord();
            return BuildPrompt(_currentWord);
        }

        var answer = userAnswer?.Trim() ?? string.Empty;
        var isSingleWord = !string.IsNullOrWhiteSpace(answer) && !answer.Contains(' ');
        if (!isSingleWord)
        {
            return "Please reply with a single word only.";
        }

        var answeredWord = _currentWord;
        var isCorrect = string.Equals(answer, answeredWord.word.Trim(), StringComparison.OrdinalIgnoreCase);
        _gameService.Evaluate(answer, answeredWord);

        _currentWord = _gameService.GetNextWord();
        var feedback = isCorrect ? "✅ Correct!" : $"❌ Incorrect. The correct word was '{answeredWord.word}'.";

        return $"{feedback}\n\n{BuildPrompt(_currentWord)}";
    }

    public Task PersistWordDifficultiesAsync()
    {
        return _gameService.PersistWordDifficultiesAsync();
    }

    private static string BuildPrompt(WordEntity wordEntity)
    {
        return "Guess the word from the clues below and reply with ONE word only.\n" +
               $"Definition: {wordEntity.definition}\n" +
               $"Example: {wordEntity.example}";
    }
}
