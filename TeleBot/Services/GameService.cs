using TeleBot.Model;

namespace TeleBot.Services;

public class GameService
{
    private int Score { get; set; }
    /** Stores the entities relevant to the game session. Adding words to the database will not update the current game session entities. */
    private List<WordEntity> currentEntities;
    
    public WordEntity GetNextWord()
    {
        return null;
    }
    
    public void HandleIncorrectAnswer(){}
    
    public void HandleCorrectAnswer(){}

    public void Evaluate(string answer, WordEntity wordEntity)
    {
        // if MCQ then an equality check will do
    }
    
    /** Algorithm to prioritize the next word that should be displayed based on the word difficulty. TODO: Give a better function name */
    private void Shuffle(){}
    
}