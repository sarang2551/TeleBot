
using TeleBot.Model.Interfaces;

namespace TeleBot.Model;

public class WordEntity : ITeleMessage
{
    private int  _id;
    public string definition;
    public string example;
    // word can be considered a unique entry in the database
    public string word;
    public int difficulty;
    public required long chat_id { get; set; }
    public required int message_id  { get; set; }
}