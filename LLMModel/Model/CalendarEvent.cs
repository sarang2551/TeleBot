using Newtonsoft.Json;

namespace LLMModel.Model;

public class CalendarEvent
{
   [JsonProperty("title")]
   public string? Title { get; set; }

   [JsonProperty("description")]
   public string? Description { get; set; }

   [JsonProperty("location")]
   public string? Location { get; set; }

   [JsonProperty("startDateTime")]
   public string? StartDateTime { get; set; }

   [JsonProperty("endDateTime")]
   public string? EndDateTime { get; set; }

   [JsonProperty("timezone")]
   public string? Timezone { get; set; }

   [JsonProperty("error")]
   public string? Error { get; set; }

   public bool IsValid => string.IsNullOrEmpty(Error) && !string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(StartDateTime);
}