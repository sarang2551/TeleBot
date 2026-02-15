namespace LLMModel.Model.Interfaces;

public interface ITeleMessage
{
    public long chat_id { get; set; }
    public int message_id { get; set; }
}