using System.Text;
using Newtonsoft.Json;

namespace LLMModel.Model;

/** Utility class to hold the calendar event data and convert it to a String for natural language event creation*/
public class CalendarEvent
{
    [JsonProperty("title")] public string? Title { get; set; }

    [JsonProperty("description")] public string? Description { get; set; }

    [JsonProperty("location")] public string? Location { get; set; }

    [JsonProperty("date")] public string? Date { get; set; }

    [JsonProperty("day")] public string? Day { get; set; }

    [JsonProperty("month")] public string? Month { get; set; }

    [JsonProperty("year")] public string? Year { get; set; }

    [JsonProperty("startDateTime")] public string? StartDateTime { get; set; }

    [JsonProperty("endDateTime")] public string? EndDateTime { get; set; }

    [JsonProperty("timezone")] public string? Timezone { get; set; }

    [JsonProperty("error")] public string? Error { get; set; }

    public bool IsValid => string.IsNullOrEmpty(Error) && !string.IsNullOrEmpty(Title) &&
                           !string.IsNullOrEmpty(StartDateTime);

    public static string JsonSchema()
    {
        const string jsonSchema = @"
            {
              ""type"": ""object"",
              ""properties"": {
                ""title"": { ""type"": [""string"", ""null""] },
                ""description"": { ""type"": [""string"", ""null""] },
                ""location"": { ""type"": [""string"", ""null""] },
                ""date"": { ""type"": [""string"", ""null""] },
                ""Day"": { ""type"": [""string"", ""null""] },
                ""Month"": { ""type"": [""string"", ""null""] },
                ""Year"": { ""type"": [""string"", ""null""] },
                ""startDateTime"": { ""type"": [""string"", ""null""] },
                ""endDateTime"": { ""type"": [""string"", ""null""] },
                ""timezone"": { ""type"": [""string"", ""null""] },
                ""error"": { ""type"": [""string"", ""null""] }
              },
              ""required"": [""title"", ""startDateTime""]
            }";
        return jsonSchema;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        // Title
        if (!string.IsNullOrEmpty(Title))
        {
            sb.Append(Title);
        }

        // Description
        if (!string.IsNullOrEmpty(Description))
        {
            sb.Append($": {Description}");
        }

        // Location
        if (!string.IsNullOrEmpty(Location))
        {
            sb.Append($" at {Location}");
        }

        // Date/Time
        if (!string.IsNullOrEmpty(StartDateTime))
        {
            sb.Append($" starting {StartDateTime}");

            if (!string.IsNullOrEmpty(EndDateTime))
            {
                sb.Append($" until {EndDateTime}");
            }
        }
        else if (!string.IsNullOrEmpty(Date))
        {
            sb.Append($" on {Date}");
        }
        else if (!string.IsNullOrEmpty(Day) || !string.IsNullOrEmpty(Month) || !string.IsNullOrEmpty(Year))
        {
            sb.Append(" on");

            if (!string.IsNullOrEmpty(Day)) sb.Append($" {Day}");
            if (!string.IsNullOrEmpty(Month)) sb.Append($" {Month}");
            if (!string.IsNullOrEmpty(Year)) sb.Append($" {Year}");
        }

        // Timezone
        if (!string.IsNullOrEmpty(Timezone))
        {
            sb.Append($" ({Timezone})");
        }

        return sb.ToString().Trim();
    }
}