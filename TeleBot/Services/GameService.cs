using TeleBot.Model;

namespace TeleBot.Services;

public class GameService
{
    private int Score { get; set; }
    /** Stores the entities relevant to the game session. Adding words to the database will not update the current game session entities. */
    private readonly List<WordEntity> currentEntities;
    private readonly FirebaseService _firebaseService;

    public GameService(FirebaseService firebaseService)
    {
        _firebaseService = firebaseService ?? throw new ArgumentNullException(nameof(firebaseService));
        var entities = _firebaseService.GetWordsAsync().GetAwaiter().GetResult();
        currentEntities = entities.OrderByDescending(entity => entity.difficulty).ToList();
    }

    public WordEntity GetNextWord()
    {
        if (currentEntities.Count == 0)
        {
            throw new InvalidOperationException("There are no words available for the current game session.");
        }

        var nextWord = currentEntities[0];
        currentEntities.RemoveAt(0);
        return nextWord;
    }

    public void HandleIncorrectAnswer(WordEntity wordEntity)
    {
        Score = Math.Max(Score - 1, 0);
        wordEntity.difficulty++;
        _firebaseService.IncrementWordDifficulty(wordEntity.word);
        RequeueWord(wordEntity);
    }

    public void HandleCorrectAnswer(WordEntity wordEntity)
    {
        Score++;
        wordEntity.difficulty = 0;
        _firebaseService.ResetWordDifficulty(wordEntity.word);
        RequeueWord(wordEntity);
    }

    public void Evaluate(string answer, WordEntity wordEntity)
    {
        // if MCQ then an equality check will do
        if (string.Equals(answer.Trim(), wordEntity.word, StringComparison.OrdinalIgnoreCase))
        {
            HandleCorrectAnswer(wordEntity);
            return;
        }

        HandleIncorrectAnswer(wordEntity);
    }

    /** Algorithm to prioritize the next word that should be displayed based on the word difficulty. TODO: Give a better function name */
    private void Shuffle(){}

    private void RequeueWord(WordEntity wordEntity)
    {
        var index = currentEntities.FindIndex(entity => entity.difficulty < wordEntity.difficulty);
        if (index == -1)
        {
            currentEntities.Add(wordEntity);
            return;
        }

        currentEntities.Insert(index, wordEntity);
    }

}
