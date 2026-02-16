using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using TeleBot.Model;

namespace TeleBot.Services;


public class FirebaseService
{
    private readonly FirebaseClient _firebaseClient;

    private class WordEntityDto
    {
        [JsonProperty("word")]
        public string word { get; set; } = string.Empty;
    
        [JsonProperty("definition")]
        public string definition { get; set; } = string.Empty;
    
        [JsonProperty("example")]
        public string example { get; set; } = string.Empty;
    
        [JsonProperty("difficulty")]
        public int difficulty { get; set; }
    }

    public FirebaseService(EnvSettings config)
    {
        String databaseUrl = config.Env.Firebase.DatabaseAddress;
        String credentialsPath = Path.Combine(AppContext.BaseDirectory, config.Env.Firebase.CredentialsPath);
        //var googleCredentials = CredentialFactory.FromFile<ServiceAccountCredential>(credentialsPath);
        var googleCredentials = GoogleCredential
            .FromFile(credentialsPath)
            .CreateScoped(
                "https://www.googleapis.com/auth/firebase.database",
                "https://www.googleapis.com/auth/userinfo.email"
            );
        _firebaseClient = new FirebaseClient(databaseUrl, 
            new FirebaseOptions{AuthTokenAsyncFactory = () => googleCredentials.UnderlyingCredential.GetAccessTokenForRequestAsync()});
    }

    public async Task<List<WordEntity>> GetWordsAsync()
    {
        try
        {
            var raw = await _firebaseClient
                .Child("")
                .OnceSingleAsync<List<WordEntityDto>>();

            if (raw == null)
            {
                Console.WriteLine("[FirebaseService] Raw response is null");
                return new List<WordEntity>();
            }

            Console.WriteLine($"[FirebaseService] Retrieved {raw.Count} words");

            return raw
                .Select(e => new WordEntity
                {
                    word = e.word,
                    definition = e.definition,
                    example = e.example,
                    difficulty = e.difficulty,
                    chat_id = 0,
                    message_id = 0
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FirebaseService] Error retrieving words: {ex.Message}");
            return new List<WordEntity>();
        }
    }

    /**
     * <summary> Function that contacts the LLMModel to generate word sentence examples and definition </summary>
     */
    public async Task AddWord(WordEntity word)
    {
        if (string.IsNullOrWhiteSpace(word.word))
        {
            throw new ArgumentException("[FirebaseService] Word is null or empty and therefore cannot be saved!");
        }

        try
        {
            var normalizedWord = word.word.Trim();
            var firebaseKey = normalizedWord.ToLowerInvariant();

            // Database schema intentionally excludes transport metadata (chat_id, message_id).
            await _firebaseClient
                .Child(string.Empty)
                .Child(firebaseKey)
                .PutAsync(new WordEntityDto
                {
                    word = normalizedWord,
                    definition = word.definition,
                    example = word.example,
                    difficulty = word.difficulty
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding word : {ex.Message}");
        }
    }

    public async Task UpdateWordDifficultiesAsync(IEnumerable<WordEntity> wordEntities)
    {
        try
        {
            var allDbWords = await _firebaseClient
                .Child(string.Empty)
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

}
