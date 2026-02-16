using TeleBot.Model;

namespace TeleBot.Services;

public class GameService
{
    private int Score { get; set; }
    /** Stores the entities relevant to the game session. Adding words to the database will not update the current game session entities. */
    private readonly List<WordEntity> currentEntities;
    private readonly FirebaseService _firebaseService;
    private WordEntity? activeWordEntity;

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
        activeWordEntity = nextWord;
        return nextWord;
    }

    public void HandleIncorrectAnswer(WordEntity wordEntity)
    {
        Score = Math.Max(Score - 1, 0);
        wordEntity.difficulty++;
        RequeueWord(wordEntity);
        ClearActiveWord(wordEntity);
    }

    public void HandleCorrectAnswer(WordEntity wordEntity)
    {
        Score++;
        wordEntity.difficulty = 0;
        RequeueWord(wordEntity);
        ClearActiveWord(wordEntity);
    }

    public void Evaluate(string answer, WordEntity wordEntity)
    {
        if (string.Equals(answer.Trim(), wordEntity.word.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            HandleCorrectAnswer(wordEntity);
            return;
        }

        HandleIncorrectAnswer(wordEntity);
    }
    
    public Task PersistWordDifficultiesAsync()
    {
        var wordsToPersist = activeWordEntity == null
            ? currentEntities
            : currentEntities.Append(activeWordEntity);

        return _firebaseService.UpdateWordDifficultiesAsync(
            wordsToPersist.DistinctBy(entity => entity.word, StringComparer.OrdinalIgnoreCase));
    }
    
    private void RequeueWord(WordEntity wordEntity)
    {
        currentEntities.Remove(wordEntity);

        var index = currentEntities.FindIndex(entity => entity.difficulty < wordEntity.difficulty);
        if (index == -1)
        {
            currentEntities.Add(wordEntity);
            return;
        }

        currentEntities.Insert(index, wordEntity);
    }

    private void ClearActiveWord(WordEntity wordEntity)
    {
        if (ReferenceEquals(activeWordEntity, wordEntity))
        {
            activeWordEntity = null;
        }
    }
    
}
