using LLMModel.Model.Interfaces;

namespace LLMModel.Model;

public class MessageRequest : BaseKafkaEntity<MessageRequest>, ITeleMessage
{
    public required string content { get; set; }
    public required int message_id { get; set; }
    public required long chat_id { get; set; }
}