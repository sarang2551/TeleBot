namespace LLMModel;

using Mistral.SDK;
using Mistral.SDK.DTOs;
using ChatMessage = Mistral.SDK.DTOs.ChatMessage;

public class MistralModelService
{
    private List<ChatMessage> ChatHistory;
    private string API_KEY;

    public MistralModelService(string apiKey)
    {
        API_KEY = apiKey;
        var systemInstructions =
            @"You are a calendar event assistant. Your task is to extract event information from natural language text and convert it to a structured JSON format for creating Google Calendar events.

            Given a natural language request, extract the following information:
            - title: The event name/title (required)
            - description: Additional details about the event (optional)
            - location: Where the event takes place (optional)
            - startDateTime: Start date and time in ISO 8601 format (YYYY-MM-DDTHH:MM:SS) (required)
            - endDateTime: End date and time in ISO 8601 format (YYYY-MM-DDTHH:MM:SS) (required)
            - timezone: Timezone identifier like 'America/New_York' or 'Asia/Singapore' (optional, default to user's timezone if not specified)

            Rules:
            1. Always respond ONLY with valid JSON, no additional text or explanations
            2. If the user doesn't specify an end time, default to 1 hour after start time
            3. If the user doesn't specify a year, assume the current year
            4. If the user specifies a relative time like 'tomorrow' or 'next week', calculate the actual date
            5. Use 24-hour time format
            6. If critical information is missing (title or start time), set 'error' field with explanation

            Current date for reference: " + DateTime.Now.ToString("yyyy-MM-dd") + @"
            Current timezone: " + TimeZoneInfo.Local.Id + @"

            Example input: 'Schedule a team meeting tomorrow at 2pm for 1 hour in Conference Room A'
            Example output:
            {
              ""title"": ""Team Meeting"",
              ""description"": """",
              ""location"": ""Conference Room A"",
              ""startDateTime"": ""2024-12-17T14:00:00"",
              ""endDateTime"": ""2024-12-17T15:00:00"",
              ""timezone"": ""Asia/Singapore""
            }

            Example input with missing info: 'Let's have a meeting'
            Example output:
            {
              ""error"": ""Missing required information: start date/time not specified""
            }

            Respond ONLY with the JSON object, nothing else.";
        
        ChatHistory = new(){new ChatMessage(ChatMessage.RoleEnum.System,systemInstructions)};
    }

    public async Task<string> GetResponse(string input)
    {
        var client = GetLlM();
        if (client == null)
        {
            Console.WriteLine("Failed to get LLM client");
            return null;
        }
        ChatHistory.Add(new ChatMessage(ChatMessage.RoleEnum.User,input));
        var request = new ChatCompletionRequest(ModelDefinitions.MistralSmall, ChatHistory);
        var response = await client.Completions.GetCompletionAsync(request);
        var output = response.Choices.First().Message.Content;
        ChatHistory.Add(new ChatMessage(ChatMessage.RoleEnum.Assistant,output));
        return output;
    }

    private MistralClient? GetLlM()
    {
        try
        {
            return new MistralClient(apiKeys: API_KEY);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
            return null;
        }
    }
}