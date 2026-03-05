using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using TeleBot.Model;

namespace TeleBot.Services;


public class FirebaseService
{
    private readonly HttpClient _httpClient;
    private readonly string _databaseUrl;
    private readonly GoogleCredential _googleCredential;

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
        _databaseUrl = config.Env.Firebase.DatabaseAddress.TrimEnd('/');
        var credentialsPath = Path.Combine(AppContext.BaseDirectory, config.Env.Firebase.CredentialsPath);

        _googleCredential = GoogleCredential
            .FromFile(credentialsPath)
            .CreateScoped(
                "https://www.googleapis.com/auth/firebase.database",
                "https://www.googleapis.com/auth/userinfo.email"
            );

        FirebaseApp.Create(new AppOptions
        {
            Credential = _googleCredential,
            DatabaseUrl = new Uri(_databaseUrl)
        }, $"telebot-{Guid.NewGuid():N}");

        _httpClient = new HttpClient();
    }

    public async Task<List<WordEntity>> GetWordsAsync()
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Get, string.Empty);
            var payload = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(payload) || payload == "null")
            {
                Console.WriteLine("[FirebaseService] Raw response is null");
                return new List<WordEntity>();
            }

            var token = JToken.Parse(payload);
            var words = token.Type switch
            {
                JTokenType.Array => token.ToObject<List<WordEntityDto>>() ?? new List<WordEntityDto>(),
                JTokenType.Object => token.ToObject<Dictionary<string, WordEntityDto>>()?.Values.ToList() ?? new List<WordEntityDto>(),
                _ => new List<WordEntityDto>()
            };

            Console.WriteLine($"[FirebaseService] Retrieved {words.Count} words");

            return words
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

            var dto = new WordEntityDto
            {
                word = normalizedWord,
                definition = word.definition,
                example = word.example,
                difficulty = word.difficulty
            };

            await SendRequestAsync(HttpMethod.Put, firebaseKey, dto);
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
            var updates = wordEntities
                .Where(entity => !string.IsNullOrWhiteSpace(entity.word))
                .ToDictionary(
                    entity => $"{entity.word.Trim().ToLowerInvariant()}/difficulty",
                    entity => (object)entity.difficulty,
                    StringComparer.OrdinalIgnoreCase);

            if (updates.Count == 0)
            {
                return;
            }

            await SendRequestAsync(new HttpMethod("PATCH"), string.Empty, updates);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating word difficulties : {ex.Message}");
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string path, object? body = null)
    {
        var token = await _googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        var endpoint = string.IsNullOrWhiteSpace(path)
            ? $"{_databaseUrl}/.json"
            : $"{_databaseUrl}/{path}.json";

        using var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body != null)
        {
            var json = JsonConvert.SerializeObject(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

}
