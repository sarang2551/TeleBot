using TeleBot.Model;

namespace TeleBot.Services;

public class GameService
{
    private int Score { get; set; }
    /** Stores the entities relevant to the game session. Adding words to the database will not update the current game session entities. */
    private readonly List<WordEntity> currentEntities;

    public GameService(IEnumerable<WordEntity>? entities = null)
    {
        currentEntities = entities?.OrderByDescending(entity => entity.difficulty).ToList() ?? [];
    }

    public WordEntity GetNextWord()
    {
        if (currentEntities.Count == 0)
        {
            throw new InvalidOperationException("There are no words available for the current game session.");
        }

        return currentEntities[0];
    }

    public void HandleIncorrectAnswer()
    {
        Score = Math.Max(Score - 1, 0);
    }

    public void HandleCorrectAnswer()
    {
        Score++;
    }

    public void Evaluate(string answer, WordEntity wordEntity)
    {
        // if MCQ then an equality check will do
        if (string.Equals(answer.Trim(), wordEntity.word, StringComparison.OrdinalIgnoreCase))
        {
            HandleCorrectAnswer();
            return;
        }

        HandleIncorrectAnswer();
    }

    /** Algorithm to prioritize the next word that should be displayed based on the word difficulty. TODO: Give a better function name */
    private void Shuffle(){}

}
