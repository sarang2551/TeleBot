using LLMModel.Model;
using LLMModel.Services.Interfaces;
using Mistral.SDK.DTOs;
using Newtonsoft.Json;

namespace LLMModel.Services.Interfaces;

public class CalendarEventUseCase : IMistralUseCase<CalendarEvent>
{
    public UseCases.UseCases Name => UseCases.UseCases.CALENDAR_EVENT;

    public ChatMessage SystemMessage { get; } = new(ChatMessage.RoleEnum.System, $@"You are a calendar event assistant. Your task is to extract event information from natural language text and convert it to a structured JSON format for creating Google Calendar events.

        Given a natural language request, extract the following information:
        - title: The event name/title (required)
        - description: Additional details about the event (optional)
        - location: Where the event takes place (optional)
        - date: The full date in YYYY-MM-DD format (optional, for reference)
        - Day: The day of the month as a string (optional)
        - Month: The month name or number as a string (optional)
        - Year: The year as a string (optional)
        - startDateTime: Start date and time in ISO 8601 format (YYYY-MM-DDTHH:MM:SS) (required)
        - endDateTime: End date and time in ISO 8601 format (YYYY-MM-DDTHH:MM:SS) (required)
        - timezone: Timezone identifier like 'America/New_York' or 'Asia/Singapore' (optional, default to user's timezone if not specified)

        JSON STRUCTURE:
        {CalendarEvent.JsonSchema()}

        Rules:
        1. Always respond ONLY with valid JSON, no additional text or explanations
        2. If start time is missing, assume the event starts at midnight
        3. If the user doesn't specify an end time, default to 24 hours after start time
        4. If the user doesn't specify a year, assume the current year
        5. If the user specifies a relative time like 'tomorrow' or 'next week', calculate the actual date
        6. Use 24-hour time format
        7. Extract Day, Month, and Year separately from the date for convenience
        8. If timezone is not specified, use user's timezone
        9. If title is missing, default to 'TeleBotEvent'
        10. If critical information is missing, set 'error' field with explanation and leave other fields null

        Current date for reference: {DateTime.Now:D}
        Current timezone: {TimeZoneInfo.Local}

        Example input: 'Schedule a team meeting tomorrow at 2pm for 1 hour in Conference Room A'
        Example output:
        {{
          ""title"": ""Team Meeting"",
          ""description"": ""Discussion about Q1 goals"",
          ""location"": ""Conference Room A"",
          ""date"": ""2025-12-26"",
          ""Day"": ""26"",
          ""Month"": ""12"",
          ""Year"": ""2025"",
          ""startDateTime"": ""2025-12-26T14:00:00"",
          ""endDateTime"": ""2025-12-26T15:00:00"",
          ""timezone"": ""Asia/Singapore"",
          ""error"": null
        }}

        Example input with missing info: 'Let's have a meeting'
        Example output:
        {{
          ""title"": null,
          ""description"": null,
          ""location"": null,
          ""date"": null,
          ""Day"": null,
          ""Month"": null,
          ""Year"": null,
          ""startDateTime"": null,
          ""endDateTime"": null,
          ""timezone"": null,
          ""error"": ""Missing required information: title and start date/time not specified""
        }}

        Respond ONLY with the JSON object, nothing else.");

    public ResponseFormat ResponseFormat { get; } = new()
    {
        Type = ResponseFormat.ResponseFormatEnum.JSON
    };

    public CalendarEvent ProcessOutput(string output)
    {
        CalendarEvent parsedResponse = JsonConvert.DeserializeObject<CalendarEvent>(output)!;
        if (!parsedResponse.IsValid)
        {
            throw new SystemException("Invalid calendar data: " + parsedResponse.Error);
        }

        Console.WriteLine("CalendarEventUseCase: Created event with title: " + parsedResponse.Title);
        return parsedResponse;
    }

    object IMistralUseCase.ProcessOutput(string output)
    {
        return ProcessOutput(output);
    }
}
