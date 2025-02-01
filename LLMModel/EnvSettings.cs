namespace LLMModel;

public class EnvSettings
{
    public required LoggingSettings Logging {get; set;}
    public required EnvObject Env {get; set;}

    public class EnvObject
    {
        public required string MISTRAL_API_KEY { get; set; }
        public required KafkaSettings Kafka { get; set; }
    }
    public class LoggingSettings
    {
        public required LogLevelSettings LogLevel { get; set; }
    }

    public class LogLevelSettings
    {
        public required string Default { get; set; }
        public required string Microsoft { get; set; }
    }

    public class KafkaSettings
    {
        public required string BootstrapServers { get; set; }
        public required string ConsumerTopic { get; set; }
        public required string ProducerTopic { get; set; }
    }
}