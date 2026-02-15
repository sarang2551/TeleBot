using Firebase.Database;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using TeleBot.Model;

namespace TeleBot.Services;

public class FirebaseService
{

    private readonly FirebaseClient _firebaseClient;
    private readonly ProducerService _producerService;

    public FirebaseService(EnvSettings config, ProducerService producerService)
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

        _producerService = producerService;
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
                result.Add(entity.Object);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving all entities : {ex.Message}");
            return new List<WordEntity>();
        }

    }

    public async Task UpdateWordDifficultiesAsync(IEnumerable<WordEntity> wordEntities)
    {
        try
        {
            var allDbWords = await _firebaseClient
                .Child("/")
                .OnceAsync<WordEntity>();

            var keyByWord = allDbWords
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Object.word))
                .GroupBy(entry => entry.Object.word, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.OrdinalIgnoreCase);

            foreach (var wordEntity in wordEntities)
            {
                if (string.IsNullOrWhiteSpace(wordEntity.word))
                {
                    continue;
                }

                if (!keyByWord.TryGetValue(wordEntity.word, out var key))
                {
                    continue;
                }

                await _firebaseClient
                    .Child("/")
                    .Child(key)
                    .Child(nameof(WordEntity.difficulty))
                    .PutAsync(wordEntity.difficulty);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating word difficulties : {ex.Message}");
        }
    }

    /**
     * <summary> Function that contacts the LLMModel to generate word sentence examples and definition </summary>
     */
    public async Task AddWord(WordEntity word)
    {
        // using kafka for a separate event trigger in the LLMModel. Fire and forget, the consumer service will handle updating the database
        return;
    }

}
