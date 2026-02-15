using Firebase.Database;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Mistral.SDK;
using TeleBot.Model;
using TeleBot.Model.Interfaces;

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
    
    public void IncrementWordDifficulty(string word)
    {
        // finds the word in the database & increases its difficulty by one
    }

    public void ResetWordDifficulty(string word)
    {
        // finds the word in the database & sets its difficulty to 0
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