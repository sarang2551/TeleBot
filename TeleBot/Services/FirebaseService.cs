using Firebase.Database;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using TeleBot.Model;

namespace TeleBot.Services;

public class FirebaseService
{

    private readonly FirebaseClient _firebaseClient;

    public FirebaseService(EnvSettings config)
    {
        String databaseUrl = config.Env.Firebase.DatabaseAddress;
        String credentialsPath = Path.Combine(AppContext.BaseDirectory, config.Env.Firebase.CredentialsPath);
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(credentialsPath)
            });
        }

        _firebaseClient = new FirebaseClient(databaseUrl);
    }

    public async Task<List<WordEntity>> GetWordsAsync()
    {
        // uses the firebase connection to return all the words in the database (not space optimized)
        try
        {
            var entities = await _firebaseClient
                .Child("/")
                .OnceAsync<WordEntity>();

            List<WordEntity> result = new List<WordEntity>();

            foreach (var entity in entities)
            {
                if (entity.Object != null)
                {
                    result.Add(entity.Object);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving all entities : {ex.Message}");
            return new List<WordEntity>();
        }

    }

    public void IncrementWordDifficulty(string word)
    {
        // finds the word in the database & increases its difficulty by one
        UpdateWordDifficulty(word, currentDifficulty => currentDifficulty + 1);
    }

    public void ResetWordDifficulty(string word)
    {
        // finds the word in the database & sets its difficulty to 0
        UpdateWordDifficulty(word, _ => 0);
    }

    /**
     * <summary> Function that contacts the LLMModel to generate word sentence examples and definition </summary>
     */
    public async Task AddWord(WordEntity word)
    {
        // Save completed word entity into Firebase using the word itself as a unique key.
        // Words without generated definition/example should not be persisted yet.
        if (string.IsNullOrWhiteSpace(word.definition) || string.IsNullOrWhiteSpace(word.example))
        {
            Console.WriteLine($"Skipping save for '{word.word}' because definition/example is missing.");
            return;
        }

        await _firebaseClient
            .Child("/")
            .Child(word.word)
            .PutAsync(word);
    }

    private void UpdateWordDifficulty(string word, Func<int, int> updateOperation)
    {
        try
        {
            UpdateWordDifficultyAsync(word, updateOperation).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating difficulty for '{word}': {ex.Message}");
        }
    }

    private async Task UpdateWordDifficultyAsync(string word, Func<int, int> updateOperation)
    {
        var entities = await _firebaseClient
            .Child("/")
            .OnceAsync<WordEntity>();

        var entityMatch = entities.FirstOrDefault(entity =>
            entity.Object != null &&
            (string.Equals(entity.Object.word, word, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(entity.Key, word, StringComparison.OrdinalIgnoreCase)));

        if (entityMatch?.Object == null)
        {
            Console.WriteLine($"Word '{word}' not found in Firebase.");
            return;
        }

        entityMatch.Object.difficulty = updateOperation(entityMatch.Object.difficulty);

        await _firebaseClient
            .Child("/")
            .Child(entityMatch.Key)
            .PutAsync(entityMatch.Object);
    }

}
