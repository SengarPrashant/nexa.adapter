namespace Nexa.Adapter.Configuration
{
    public class LLMOptions
    {
        public string Provider { get; set; }

        public BedrockOptions Bedrock { get; set; }

        public AzureOptions Azure { get; set; }
    }

    public class BedrockOptions
    {
        public string ModelId { get; set; }
        public string Region { get; set; }
        public int MaxTokens { get; set; }
    }

    public class AzureOptions
    {
        public string Endpoint { get; set; }
        public string DeploymentName { get; set; }
        public string ApiKey { get; set; }

        public int MaxTokens { get; set; }
    }
}
