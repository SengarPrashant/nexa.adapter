namespace Nexa.Adapter.Infrastructure.LLM
{
    public class LLMProviderFactory
    {
        public static void Register(IServiceCollection services, IConfiguration config)
        {
            var provider = config["LLM:Provider"];

            switch (provider)
            {
                case "Bedrock":
                    services.AddSingleton<ILLMProvider, BedrockProvider>();
                    break;

                case "Azure":
                    services.AddSingleton<ILLMProvider, AzureOpenAIProvider>();
                    break;

                default:
                    throw new Exception("Unsupported LLM Provider");
            }
        }
    }
}
