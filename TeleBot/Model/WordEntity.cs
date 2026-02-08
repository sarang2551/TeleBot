
namespace TeleBot.Model;

public record WordEntity
{
    private int  _id;
    public string definition;
    public string example;
    // word can be considered a unique entry in the database
    public string word;
    public int difficulty;
}