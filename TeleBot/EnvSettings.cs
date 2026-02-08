namespace TeleBot;

public class EnvSettings
{
    public required LoggingSettings Logging {get; set;}
    public required EnvObject Env {get; set;}

    public class EnvObject
    {
        public required string BOT_TOKEN { get; set; }
        public required Kafka Kafka { get; set; }
        public required Firebase Firebase { get; set; }
        public required Wordgame Wordgame { get; set; }
    }
    public class LoggingSettings
    {
        public required LogLevelSettings LogLevel { get; set; }
    }

    public class LogLevelSettings
    {
        public required string Default { get; set; }
        public required string System { get; set; }
        public required string Microsoft { get; set; }
    }

    public class Kafka
    {
        public required string BootstrapServers { get; set; }
        public required string ConsumerTopic { get; set; }
        public required string ProducerTopic { get; set; }
    }

    public class Firebase
    {
        public required string CredentialsPath { get; set; }
        public required string DatabaseAddress { get; set; }
    }

    public class Wordgame
    {
        public required string ProducerTopic { get; set; }
        public required string ConsumerTopic { get; set; }
    }
}